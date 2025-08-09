using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Respawn;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Handles player spawning and cleanup on peer connect/disconnect events.
    /// Listens for network events and creates or destroys player entities accordingly.
    /// </summary>
    public class PlayerSpawnHandler(
        EventBasedNetListener netEventBroadcaster,
        EntityRegistry entityRegistry,
        ILogger logger)
        : IInitializable, IDisposable
    {
        /// <summary>
        /// Subscribes to peer connection events.
        /// </summary>
        public void Initialize()
        {
            netEventBroadcaster.PeerConnectedEvent += OnPeerConnected;
            netEventBroadcaster.PeerDisconnectedEvent += OnPeerDisconnected;
        }

        /// <summary>
        /// Unsubscribes from peer connection events.
        /// </summary>
        public void Dispose()
        {
            netEventBroadcaster.PeerConnectedEvent -= OnPeerConnected;
            netEventBroadcaster.PeerDisconnectedEvent -= OnPeerDisconnected;
        }

        /// <summary>
        /// Handles spawning a player entity when a peer connects.
        /// </summary>
        /// <param name="peer">The connected network peer.</param>
        private void OnPeerConnected(NetPeer peer)
        {
            logger.Info(LoggedFeature.Player, "Handling player spawn request from peer {0}", peer.Id);

            var x = Random.Shared.Next(-3, 3);
            var y = 0;
            var z = Random.Shared.Next(-3, 3);

            try
            {
                var playerEntity = PlayerArchetype.Create(
                    entityRegistry,
                    peer.Id,
                    new System.Numerics.Vector3(x, y, z));

                logger.Info(LoggedFeature.Player, "Created player entity {0} for peer {1}", playerEntity.Id, peer.Id);
            }
            catch (Exception ex)
            {
                logger.Error(LoggedFeature.Player, "Failed to create player entity for client {0}: {1}", peer.Id, ex.Message);
            }
        }

        /// <summary>
        /// Handles cleanup of player entities when a peer disconnects.
        /// </summary>
        /// <param name="peer">The disconnected network peer.</param>
        /// <param name="disconnectInfo">Information about the disconnect.</param>
        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            var playerEntity = entityRegistry.GetPlayerEntity(peer.Id);
            if (playerEntity == null)
            {
                // Check if the player is "despawned" (e.g., dead)
                playerEntity = entityRegistry.GetRespawningPlayer(peer.Id);
                if (playerEntity == null)
                {
                    logger.Warn(LoggedFeature.Player, "No player entity found for peer {0} on disconnect", peer.Id);
                    return;
                }
            }

            // Remove the player entity from the registry
            entityRegistry.DestroyEntity(playerEntity.Id);
            logger.Info(LoggedFeature.Player, "Removed player entity {0} for peer {1}", playerEntity.Id, peer.Id);
        }
    }
}