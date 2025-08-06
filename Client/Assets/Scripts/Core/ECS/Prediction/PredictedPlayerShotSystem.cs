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
using Shared.Logging;
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
        
        // Cooldown tracking
        private uint _lastShotTick;

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

        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            AssociateServerProjectiles(entityRegistry);
        }
        
        public void Dispose()
        {
            _inputListener.OnShoot -= HandleShootInput;
        }
        
        private void HandleShootInput()
        {
            var clientTick = _tickSync.ClientTick;
            
            // Check cooldown
            if (clientTick < _lastShotTick + GameplayConstants.PlayerShotCooldownTicks)
            {
                return;
            }
            _lastShotTick = clientTick;

            var shotDirection = Vector3.UnitZ;
            var localPlayer = _entityRegistry.GetLocalPlayerEntity(_localPeerId);
            var playerPosition = localPlayer.GetRequired<PositionComponent>();
            var firePosition = playerPosition.Value;

            // Generate unique ID for this predicted projectile
            var predictedProjectileId = Guid.NewGuid();

            // Create predicted projectile entity
            var projectile = CreatePredictedProjectile(_entityRegistry, firePosition, shotDirection, clientTick, predictedProjectileId);

            // Track the predicted projectile
            _predictedProjectiles[predictedProjectileId] = projectile;

            // Send shot message to server
            var predictedServerTick = _tickSync.ClientTick - _tickSync.TickOffset;
            SendShotMessage((uint)predictedServerTick, shotDirection, predictedProjectileId);

            _logger.Debug("Fired predicted projectile {0} at tick {1}", predictedProjectileId, clientTick);
        }

        private Entity CreatePredictedProjectile(EntityRegistry entityRegistry, Vector3 position, Vector3 direction, uint currentTick, Guid predictedId)
        {
            var projectile = entityRegistry.CreateEntity();
            
            // Position and movement
            projectile.AddPredictedComponent(new PositionComponent { Value = position });
            projectile.AddPredictedComponent(new VelocityComponent { Value = direction * GameplayConstants.ProjectileSpeed });
            
            // Projectile properties
            // We do NOT add SpawnAuthorityComponent here, as this is a predicted entity.
            // The server will create the authoritative entity when it processes the shot.
            // and the SpawnAuthorityComponent will be added to it.
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new PrefabComponent { PrefabName = GameplayConstants.ProjectilePrefabName });
            projectile.AddComponent(new DamageApplyingComponent { Damage = GameplayConstants.ProjectileDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(currentTick, GameplayConstants.ProjectileTtlTicks));
            
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
                _logger.Debug(LoggedFeature.Input, "Sent shot message for tick {0}", tick);
            }
            catch (Exception ex)
            {
                _logger.Error(LoggedFeature.Input, "Failed to send shot message: {0}", ex.Message);
            }
        }

        private void AssociateServerProjectiles(EntityRegistry entityRegistry)
        {
            // Find server projectiles that need to be associated with predicted ones
            var serverProjectiles = entityRegistry
                .GetAll()
                .Where(x => x.Has<SpawnAuthorityComponent>() && x.Has<ProjectileTagComponent>())
                .ToList();

            foreach (var serverProjectile in serverProjectiles)
            {
                var spawnAuthority = serverProjectile.GetRequired<SpawnAuthorityComponent>();

                // Check if this server projectile corresponds to one of our predictions
                if (spawnAuthority.SpawnedByPeerId == _localPeerId && 
                    _predictedProjectiles.TryGetValue(spawnAuthority.LocalEntityId, out var predictedProjectile))
                {
                    // The server has confirmed our shot. We can now remove our predicted projectile.
                    entityRegistry.DestroyEntity(predictedProjectile.Id);
                    _predictedProjectiles.Remove(spawnAuthority.LocalEntityId);

                    _logger.Debug(LoggedFeature.Prediction, "Associated server projectile {0} with predicted projectile {1}", serverProjectile.Id, spawnAuthority.LocalEntityId);
                }
            }
        }
    }
}