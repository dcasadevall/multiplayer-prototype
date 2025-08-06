using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.ECS.Entities;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Scheduling;
using ILogger = Shared.Logging.ILogger;

namespace Core.ECS.Prediction
{
    /// <summary>
    /// System that handles client-side projectile prediction.
    /// Spawns predicted projectiles when the local player shoots, and associates them with server projectiles.
    /// </summary>
    public class PredictedPlayerShotSystem : ISystem, IInitializable, IDisposable
    {
        private readonly IInputListener _inputListener;
        private readonly EntityRegistry _entityRegistry;
        private readonly IMessageSender _messageSender;
        private readonly ITickSync _tickSync;
        private readonly int _localPeerId;
        private readonly ILogger _logger;
        
        // Track predicted projectiles for association with server entities
        private readonly Dictionary<Guid, Entity> _predictedProjectiles = new();
        private readonly Dictionary<Guid, Guid> _serverToLocalMapping = new();
        
        // Projectile settings
        private const float LaserSpeed = 15f;
        private const uint LaserTTLTicks = 120; // 4 seconds at 30 ticks/sec
        private const int LaserDamage = 25;

        public PredictedPlayerShotSystem(
            IInputListener inputListener,
            EntityRegistry entityRegistry,
            IMessageSender messageSender,
            IClientConnection clientConnection,
            ITickSync tickSync,
            ILogger logger)
        {
            _inputListener = inputListener;
            _entityRegistry = entityRegistry;
            _messageSender = messageSender;
            _tickSync = tickSync;
            _logger = logger;
            _localPeerId = clientConnection.AssignedPeerId;
        }

        public void Initialize()
        {
            _inputListener.OnShoot += HandleShootInput;
        }

        public void Dispose()
        {
            _inputListener.OnShoot -= HandleShootInput;
        }
        
        private void HandleShootInput()
        {
            var clientTick = _tickSync.ClientTick;
            CreatePredictedProjectile(_entityRegistry, Vector3.UnitZ, clientTick);
        }

        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            var localPlayer = entityRegistry.GetLocalPlayerEntity(_localPeerId);
            if (localPlayer == null)
            {
                return;
            }
            
            UpdateProjectileMovement(entityRegistry, deltaTime);
            AssociateServerProjectiles(entityRegistry);
        }

        private void CreatePredictedProjectile(EntityRegistry entityRegistry, Vector3 shotDirection, uint currentTick)
        {
            var localPlayer = entityRegistry.GetLocalPlayerEntity(_localPeerId);
            var playerPosition = localPlayer.GetRequired<PositionComponent>();
            var firePosition = playerPosition.Value;

            // Generate unique ID for this predicted projectile
            var predictedProjectileId = Guid.NewGuid();

            // Create predicted projectile entity
            var projectile = CreatePredictedProjectile(entityRegistry, firePosition, shotDirection, currentTick, predictedProjectileId);

            // Track the predicted projectile
            _predictedProjectiles[predictedProjectileId] = projectile;

            // Send shot message to server
            SendShotMessage(currentTick, shotDirection, predictedProjectileId);

            _logger.Debug("Fired predicted projectile {0} at tick {1}", predictedProjectileId, currentTick);
        }

        private Entity CreatePredictedProjectile(EntityRegistry entityRegistry, Vector3 position, Vector3 direction, uint currentTick, Guid predictedId)
        {
            var projectile = entityRegistry.CreateEntity();
            
            // Position and movement
            projectile.AddComponent(new PositionComponent { X = position.X, Y = position.Y, Z = position.Z });
            projectile.AddComponent(new VelocityComponent { X = direction.X * LaserSpeed, Y = direction.Y * LaserSpeed, Z = direction.Z * LaserSpeed });
            
            // Projectile properties
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new DamageApplyingComponent { Damage = LaserDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(currentTick, LaserTTLTicks));
            
            // Spawn authority
            projectile.AddComponent(new SpawnAuthorityComponent 
            { 
                SpawnedByPeerId = _localPeerId, 
                LocalEntityId = predictedId, 
                SpawnTick = currentTick 
            });
            
            // Make it replicated (will be overridden by server data when it arrives)
            projectile.AddComponent(new ReplicatedTagComponent());
            
            return projectile;
        }

        private void SendShotMessage(uint tick, Vector3 fireDirection, Guid predictedProjectileId)
        {
            var shotMessage = new PlayerShotMessage
            {
                Tick = tick,
                Direction = fireDirection,
                PredictedProjectileId = predictedProjectileId
            };
            
            try
            {
                _messageSender.SendMessageToServer(MessageType.PlayerShot, shotMessage);
                _logger.Debug("Sent shot message for tick {0}", tick);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to send shot message: {0}", ex.Message);
            }
        }

        private void UpdateProjectileMovement(EntityRegistry entityRegistry, float deltaTime)
        {
            var projectiles = entityRegistry
                .GetAll()
                .Where(x => x.Has<ProjectileTagComponent>())
                .Where(x => x.Has<PositionComponent>())
                .Where(x => x.Has<VelocityComponent>())
                .ToList();

            foreach (var projectile in projectiles)
            {
                var position = projectile.GetRequired<PositionComponent>();
                var velocity = projectile.GetRequired<VelocityComponent>();
                
                // Update position based on velocity
                position.Value += velocity.Value * deltaTime;
            }
        }

        private void AssociateServerProjectiles(EntityRegistry entityRegistry)
        {
            // Find server projectiles that need to be associated with predicted ones
            var serverProjectiles = entityRegistry
                .GetAll()
                .Where(x => x.Has<SpawnAuthorityComponent>())
                .Where(x => x.Has<ProjectileTagComponent>())
                .Where(x => !x.TryGet<PredictedComponent<PositionComponent>>(out _)) // Server projectiles don't have prediction wrappers
                .ToList();

            foreach (var serverProjectile in serverProjectiles)
            {
                var spawnAuthority = serverProjectile.GetRequired<SpawnAuthorityComponent>();
                
                // Check if this server projectile corresponds to one of our predictions
                // Since we removed IsPredicted, we check if the entity doesn't have a PredictedComponent wrapper
                var isPredicted = serverProjectile.TryGet<PredictedComponent<PositionComponent>>(out _);
                if (!isPredicted && spawnAuthority.SpawnedByPeerId == _localPeerId &&
                    _predictedProjectiles.TryGetValue(spawnAuthority.LocalEntityId, out var predictedProjectile))
                {
                    // Associate the server entity with our predicted entity
                    _serverToLocalMapping[serverProjectile.Id.Value] = spawnAuthority.LocalEntityId;
                    
                    // Remove the predicted projectile since we now have server authority
                    entityRegistry.DestroyEntity(predictedProjectile.Id);
                    _predictedProjectiles.Remove(spawnAuthority.LocalEntityId);
                    
                    _logger.Debug("Associated server projectile {0} with predicted projectile {1}", 
                        serverProjectile.Id, spawnAuthority.LocalEntityId);
                }
            }
        }
    }
}