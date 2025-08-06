using System;
using System.Collections.Generic;
using System.Linq;
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
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Core.ECS.Prediction
{
    /// <summary>
    /// System that handles client-side projectile prediction.
    /// Spawns predicted projectiles when the local player shoots, and associates them with server projectiles.
    /// </summary>
    public class ProjectilePredictionSystem : ISystem
    {
        private readonly IMessageSender _messageSender;
        private readonly IClientConnection _clientConnection;
        private readonly ITickSync _tickSync;
        private readonly ILogger _logger;
        
        // Track predicted projectiles for association with server entities
        private readonly Dictionary<Guid, Entity> _predictedProjectiles = new();
        private readonly Dictionary<Guid, Guid> _serverToLocalMapping = new();
        
        // Projectile settings
        private const float LaserSpeed = 15f;
        private const uint LaserTTLTicks = 120; // 4 seconds at 30 ticks/sec
        private const int LaserDamage = 25;

        public ProjectilePredictionSystem(
            IInputListener inputListener,
            IMessageSender messageSender,
            IClientConnection clientConnection,
            ITickSync tickSync,
            ILogger logger)
        {
            _messageSender = messageSender;
            _clientConnection = clientConnection;
            _tickSync = tickSync;
            _logger = logger;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            HandleShootInput(registry);
            UpdateProjectileMovement(registry, deltaTime);
            AssociateServerProjectiles(registry);
        }

        private void HandleShootInput(EntityRegistry registry)
        {
            var currentTick = _tickSync.ClientTick;
            
            // Check for shoot input (space bar)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var localPlayer = GetLocalPlayerEntity(registry);
                if (localPlayer != null)
                {
                    FireProjectile(registry, localPlayer, currentTick);
                }
            }
        }

        private Entity? GetLocalPlayerEntity(EntityRegistry registry)
        {
            return registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PositionComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == _clientConnection.AssignedPeerId);
        }

        private void FireProjectile(EntityRegistry registry, Entity player, uint currentTick)
        {
            var playerPosition = player.GetRequired<PositionComponent>();
            var firePosition = playerPosition.Value;
            var fireDirection = Vector3.UnitZ; // Forward direction (can be enhanced with camera direction later)
            
            // Generate unique ID for this predicted projectile
            var predictedProjectileId = Guid.NewGuid();
            
            // Create predicted projectile entity
            var projectile = CreatePredictedProjectile(registry, firePosition, fireDirection, currentTick, predictedProjectileId);
            
            // Track the predicted projectile
            _predictedProjectiles[predictedProjectileId] = projectile;
            
            // Send shot message to server
            SendShotMessage(currentTick, firePosition, fireDirection, predictedProjectileId);
            
            _logger.Debug("Fired predicted projectile {0} at tick {1}", predictedProjectileId, currentTick);
        }

        private Entity CreatePredictedProjectile(EntityRegistry registry, Vector3 position, Vector3 direction, uint currentTick, Guid predictedId)
        {
            var projectile = registry.CreateEntity();
            
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
                SpawnedByPeerId = _clientConnection.AssignedPeerId, 
                LocalEntityId = predictedId, 
                SpawnTick = currentTick 
            });
            
            // Make it replicated (will be overridden by server data when it arrives)
            projectile.AddComponent(new ReplicatedTagComponent());
            
            return projectile;
        }

        private void SendShotMessage(uint tick, Vector3 firePosition, Vector3 fireDirection, Guid predictedProjectileId)
        {
            var shotMessage = new PlayerShotMessage
            {
                Tick = tick,
                FirePosition = firePosition,
                FireDirection = fireDirection,
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

        private void UpdateProjectileMovement(EntityRegistry registry, float deltaTime)
        {
            var projectiles = registry
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

        private void AssociateServerProjectiles(EntityRegistry registry)
        {
            // Find server projectiles that need to be associated with predicted ones
            var serverProjectiles = registry
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
                if (!isPredicted && spawnAuthority.SpawnedByPeerId == _clientConnection.AssignedPeerId &&
                    _predictedProjectiles.TryGetValue(spawnAuthority.LocalEntityId, out var predictedProjectile))
                {
                    // Associate the server entity with our predicted entity
                    _serverToLocalMapping[serverProjectile.Id.Value] = spawnAuthority.LocalEntityId;
                    
                    // Remove the predicted projectile since we now have server authority
                    registry.DestroyEntity(predictedProjectile.Id);
                    _predictedProjectiles.Remove(spawnAuthority.LocalEntityId);
                    
                    _logger.Debug("Associated server projectile {0} with predicted projectile {1}", 
                        serverProjectile.Id, spawnAuthority.LocalEntityId);
                }
            }
        }

        /// <summary>
        /// Cleans up prediction tracking for old projectiles
        /// </summary>
        public void CleanupOldPredictions(uint currentTick)
        {
            var oldPredictions = _predictedProjectiles
                .Where(kvp => kvp.Value.Has<SelfDestroyingComponent>())
                .Where(kvp => kvp.Value.GetRequired<SelfDestroyingComponent>().DestroyAtTick < currentTick)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var predictionId in oldPredictions)
            {
                _predictedProjectiles.Remove(predictionId);
            }
        }
    }
}