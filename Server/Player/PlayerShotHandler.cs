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

        // Track last shot time per peer for cooldown validation
        private readonly Dictionary<int, uint> _lastShotTicks = new();

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

        public void HandlePlayerShot(int peerId, PlayerShotMessage shotMessage)
        {
            try
            {
                // Validate the shot
                if (!ValidateShot(peerId, shotMessage))
                {
                    logger.Warn("Invalid shot from peer {0}: validation failed", peerId);
                    return;
                }

                var playerEntity = GetPlayerEntity(peerId);
                if (playerEntity == null)
                {
                    logger.Warn("Player {0} does not have a player entity", peerId);
                    return;
                }

                // Record the shot time for cooldown tracking
                _lastShotTicks[peerId] = shotMessage.Tick;

                // Spawn the authoritative projectile
                var projectile = SpawnProjectile(shotMessage, playerEntity);

                logger.Debug("Spawned projectile {0} for peer {1} at tick {2}",
                    projectile.Id, peerId, shotMessage.Tick);
            }
            catch (Exception ex)
            {
                logger.Error("Error handling player shot from peer {0}: {1}", peerId, ex.Message);
            }
        }

        private bool ValidateShot(int peerId, PlayerShotMessage shotMessage)
        {
            // Get current server tick
            var serverTick = entityRegistry.GetAll()
                .First(x => x.Has<ServerTickComponent>())
                .GetRequired<ServerTickComponent>().TickNumber;

            // Validate tick (shouldn't be too far in the future or past
            if (shotMessage.Tick > serverTick + GameplayConstants.MaxShotTickDeviation ||
                shotMessage.Tick < serverTick - GameplayConstants.MaxShotTickDeviation)
            {
                logger.Warn("Shot tick {0} is too far in the future (current: {1})", shotMessage.Tick, serverTick);
                return false;
            }

            // Validate cooldown - prevent shot spamming
            if (_lastShotTicks.TryGetValue(peerId, out var lastShotTick))
            {
                if (shotMessage.Tick < lastShotTick + GameplayConstants.PlayerShotCooldownTicks)
                {
                    logger.Warn("Shot from peer {0} blocked by server cooldown. Last shot: {1}, Current: {2}",
                        peerId, lastShotTick, shotMessage.Tick);
                    return false;
                }
            }

            // Validate direction (should be normalized)
            if (Math.Abs(shotMessage.Direction.Length() - 1.0f) > 0.1f)
            {
                logger.Warn("Invalid fire direction magnitude: {0}", shotMessage.Direction.Length());
                return false;
            }

            return true;
        }

        private Entity SpawnProjectile(PlayerShotMessage shotMessage, Entity playerEntity)
        {
            var projectile = entityRegistry.CreateEntity();
            var playerPosition = playerEntity.GetRequired<PositionComponent>();
            var peerId = playerEntity.GetRequired<PeerComponent>().PeerId;

            // Get current server tick
            var serverTick = entityRegistry.GetAll()
                .First(x => x.Has<ServerTickComponent>())
                .GetRequired<ServerTickComponent>().TickNumber;

            // Position and movement
            projectile.AddComponent(new PositionComponent
            {
                X = playerPosition.X,
                Y = playerPosition.Y,
                Z = playerPosition.Z
            });

            // Add initial velocity based on shot direction
            var velocity = shotMessage.Direction * GameplayConstants.ProjectileSpeed;
            projectile.AddComponent(new VelocityComponent
            {
                X = velocity.X,
                Y = velocity.Y,
                Z = velocity.Z
            });

            // Projectile properties
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new DamageApplyingComponent { Damage = GameplayConstants.ProjectileDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(serverTick, GameplayConstants.ProjectileTtlTicks));

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
        /// Cleans up shot tracking data for a disconnected peer.
        /// </summary>
        /// <param name="peerId">The peer ID that disconnected</param>
        public void OnPeerDisconnected(int peerId)
        {
            _lastShotTicks.Remove(peerId);
            logger.Debug("Cleaned up shot tracking for disconnected peer {0}", peerId);
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