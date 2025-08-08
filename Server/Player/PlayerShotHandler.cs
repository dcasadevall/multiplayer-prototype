using Shared;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Archetypes;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Physics;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Listens for <see cref="PlayerShotMessage"/> messages from the network and handles projectile spawning.
    /// Validates shots and spawns authoritative projectiles.
    /// </summary>
    public class PlayerShotHandler(
        EntityRegistry entityRegistry,
        IMessageReceiver messageReceiver,
        ITickSync tickSync,
        ILogger logger)
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

                var playerEntity = entityRegistry.GetPlayerEntity(peerId);
                if (playerEntity == null)
                {
                    logger.Warn("Player {0} does not have a player entity", peerId);
                    return;
                }

                // Record the shot time for cooldown tracking
                _lastShotTicks[peerId] = shotMessage.Tick;

                // Spawn the authoritative projectile
                // Currently, it spawns at the server position for the player at the
                // tick of receiving the shot message.
                // we need a buffer of player positions at X tick so we can
                // spawn the projectile at the correct position.
                var projectile = ProjectileArchetype.CreateFromPlayer(
                    entityRegistry,
                    playerEntity,
                    tickSync.ServerTick);

                // Server adds the spawn authority component
                // This will cause the local client to destroy its predicted projectile
                projectile.AddComponent(new SpawnAuthorityComponent
                {
                    SpawnedByPeerId = peerId,
                    LocalEntityId = shotMessage.PredictedProjectileId,
                    SpawnTick = shotMessage.Tick
                });

                var rot = playerEntity.GetRequired<RotationComponent>().Value;
                var pos = projectile.GetRequired<PositionComponent>().Value;
                var vel = projectile.GetRequired<VelocityComponent>().Value;
                logger.Info($"[Debug] Player Rotation: {rot.X:F2}, {rot.Y:F2}, {rot.Z:F2}, {rot.W:F2}");
                logger.Info($"[Debug] Projectile Spawn Pos: {pos.X:F2}, {pos.Y:F2}, {pos.Z:F2}");
                logger.Info($"[Debug] Projectile Velocity: {vel.X:F2}, {vel.Y:F2}, {vel.Z:F2}");


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
            var serverTick = tickSync.ServerTick;

            // // Validate tick (shouldn't be too far in the future or past
            if (shotMessage.Tick > serverTick + GameplayConstants.MaxShotTickDeviation ||
                shotMessage.Tick < serverTick - GameplayConstants.MaxShotTickDeviation)
            {
                logger.Warn("Shot tick {0} is too far out of sync (current: {1})", shotMessage.Tick, serverTick);
                return false;
            }

            // Validate cooldown - prevent shot spamming
            if (_lastShotTicks.TryGetValue(peerId, out var lastShotTick))
            {
                if (shotMessage.Tick < lastShotTick + GameplayConstants.PlayerShotCooldown.ToNumTicks())
                {
                    logger.Warn("Shot from peer {0} blocked by server cooldown. Last shot: {1}, Current: {2}",
                        peerId, lastShotTick, shotMessage.Tick);
                    return false;
                }
            }

            return true;
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
    }
}