using System;
using System.Linq;
using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Listens for <see cref="PlayerShotMessage"/> messages from the network and handles projectile spawning.
    /// Validates shots and spawns authoritative projectiles.
    /// </summary>
    public class PlayerShotHandler(EntityRegistry entityRegistry, IMessageReceiver messageReceiver, ILogger logger)
        : IInitializable, IDisposable
    {
        private IDisposable? _subscription;

        // Projectile settings (should match client)
        private const float LaserSpeed = 15f;
        private const uint LaserTTLTicks = 120; // 4 seconds at 30 ticks/sec
        private const int LaserDamage = 25;
        private const float MaxShotRange = 100f; // Maximum valid shot distance

        public void Initialize()
        {
            logger.Debug("PlayerShotListener initialized and registered for PlayerShotMessage");
            _subscription = messageReceiver.RegisterMessageHandler<PlayerShotMessage>(GetType().Name, HandlePlayerShotMessage);
        }

        /// <summary>
        /// Handles incoming <see cref="PlayerShotMessage"/> messages and delegates to the shot handler.
        /// </summary>
        /// <param name="peerId">The network peer ID of the sender.</param>
        /// <param name="msg">The shot message containing firing data.</param>
        private void HandlePlayerShotMessage(int peerId, PlayerShotMessage msg)
        {
            try
            {
                logger.Debug("Received PlayerShotMessage from peer {0} at tick {1}", peerId, msg.Tick);
                HandlePlayerShot(peerId, msg);
            }
            catch (Exception ex)
            {
                logger.Error("Error handling PlayerShotMessage from peer {0}: {1}", peerId, ex.Message);
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void HandlePlayerShot(int peerId, PlayerShotMessage shotMessage)
        {
            try
            {
                // Validate the shot
                // Get the server tick entity
                var serverTick = entityRegistry.GetAll()
                    .First(x => x.Has<ServerTickComponent>())
                    .GetRequired<ServerTickComponent>().TickNumber;

                var playerEntity = GetPlayerEntity(peerId);
                if (playerEntity == null)
                {
                    logger.Warn(LoggedFeature.Input, $"Player {peerId} does not have a player entity");
                    return;
                }

                if (!ValidateShot(shotMessage, serverTick))
                {
                    logger.Warn(LoggedFeature.Input, "Invalid shot from peer {0}: validation failed", peerId);
                    return;
                }

                // Spawn the authoritative projectile
                var projectile = SpawnProjectile(shotMessage, serverTick, playerEntity);

                logger.Debug(LoggedFeature.Input, "Spawned projectile {0} for peer {1} at tick {2}",
                    projectile.Id, peerId, shotMessage.Tick);
            }
            catch (Exception ex)
            {
                logger.Error(LoggedFeature.Input, "Error handling player shot from peer {0}: {1}", peerId, ex.Message);
            }
        }

        private bool ValidateShot(PlayerShotMessage shotMessage, uint serverTick)
        {
            // Validate tick (shouldn't be too far in the future)
            if (shotMessage.Tick > serverTick + 10) // Allow some tolerance for latency
            {
                logger.Warn(LoggedFeature.Input, "Shot tick {0} is too far in the future (current: {1})", shotMessage.Tick, serverTick);
                return false;
            }

            // Validate direction (should be normalized)
            if (Math.Abs(shotMessage.FireDirection.Length() - 1.0f) > 0.1f)
            {
                logger.Warn(LoggedFeature.Input, "Invalid fire direction magnitude: {0}", shotMessage.FireDirection.Length());
                return false;
            }

            return true;
        }

        private Entity SpawnProjectile(PlayerShotMessage shotMessage, uint serverTick, Entity playerEntity)
        {
            var projectile = entityRegistry.CreateEntity();
            var playerPosition = playerEntity.GetRequired<PositionComponent>().Value;
            var peerId = playerEntity.GetRequired<PeerComponent>().PeerId;

            // Position and movement
            projectile.AddComponent(new PositionComponent
            {
                X = playerPosition.X,
                Y = playerPosition.Y,
                Z = playerPosition.Z
            });

            // Add initial velocity based on shot direction
            var velocity = shotMessage.FireDirection * LaserSpeed;
            projectile.AddComponent(new VelocityComponent
            {
                X = velocity.X,
                Y = 0,
                Z = velocity.Y
            });

            // Projectile properties
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new DamageApplyingComponent { Damage = LaserDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(serverTick, LaserTTLTicks));

            // Spawn authority
            projectile.AddComponent(new SpawnAuthorityComponent
            {
                SpawnedByPeerId = peerId,
                LocalEntityId = shotMessage.PredictedProjectileId,
                SpawnTick = shotMessage.Tick
            });

            // Make it replicated so other clients receive it
            projectile.AddComponent(new ReplicatedTagComponent());

            return projectile;
        }

        /// <summary>
        /// Gets the player entity for the given peer ID.
        /// Used for validation and position checking.
        /// </summary>
        private Entity? GetPlayerEntity(int peerId)
        {
            return entityRegistry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == peerId);
        }
    }
}