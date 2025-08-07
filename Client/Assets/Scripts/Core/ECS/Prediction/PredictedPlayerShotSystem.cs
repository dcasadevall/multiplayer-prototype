using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.ECS.Entities;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Archetypes;
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

            var localPlayer = _entityRegistry.GetLocalPlayerEntity(_localPeerId);
            var playerPosition = localPlayer.GetRequired<PositionComponent>();
            var playerRotation = localPlayer.GetRequired<RotationComponent>();
            var shotDirection = Vector3.Transform(Vector3.UnitZ, playerRotation.Value);

            // Create predicted projectile entity
            var projectile = ProjectileArchetype.Create(
                _entityRegistry,
                playerPosition.Value,
                shotDirection * GameplayConstants.ProjectileSpeed,
                clientTick,
                _localPeerId);
            
            var predictedProjectileId = projectile.Id;

            // Track the predicted projectile
            _predictedProjectiles[predictedProjectileId.Value] = projectile;

            // Send shot message to server
            SendShotMessage(clientTick, shotDirection, predictedProjectileId.Value);

            _logger.Debug("Fired predicted projectile {0} at tick {1}", predictedProjectileId, _tickSync.ServerTick);
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