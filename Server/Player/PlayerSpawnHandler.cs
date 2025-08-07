using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.Logging;
using Shared.Scheduling;

namespace Server.Player
{
    public class PlayerSpawnHandler(
        EventBasedNetListener netEventBroadcaster,
        EntityRegistry entityRegistry,
        ILogger logger)
        : IInitializable, IDisposable
    {
        private readonly Dictionary<int, EntityId> _peerEntityMap = new();

        public void Initialize()
        {
            netEventBroadcaster.PeerConnectedEvent += OnPeerConnected;
            netEventBroadcaster.PeerDisconnectedEvent += OnPeerDisconnected;
        }

        public void Dispose()
        {
            netEventBroadcaster.PeerConnectedEvent -= OnPeerConnected;
            netEventBroadcaster.PeerDisconnectedEvent -= OnPeerDisconnected;
        }

        private void OnPeerConnected(NetPeer peer)
        {
            logger.Info("Handling player spawn request from peer {0}", peer.Id);

            // Generate a spawn position (this could be more complex in a real game)
            // We can keep it within a radius of 10 units from the origin for simplicity
            var x = Random.Shared.Next(-3, 3);
            var y = 0;
            var z = Random.Shared.Next(-3, 3);

            try
            {
                var name = $"Player_{peer.Id}";
                var playerEntity = PlayerArchetype.Create(
                    entityRegistry,
                    peer.Id,
                    name,
                    new System.Numerics.Vector3(x, y, z));

                logger.Info(LoggedFeature.Networking, "Created player entity {0} for peer {1}", playerEntity.Id, peer.Id);

                // Store the mapping of peer ID to entity ID
                _peerEntityMap[peer.Id] = playerEntity.Id;
            }
            catch (Exception ex)
            {
                logger.Error(LoggedFeature.Networking, "Failed to create player entity for client {0}: {1}", peer.Id, ex.Message);
            }
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (!_peerEntityMap.TryGetValue(peer.Id, out var entityId))
            {
                logger.Warn(LoggedFeature.Networking, "No player entity found for disconnected peer {0}", peer.Id);
                return;
            }

            // Remove the player entity from the registry
            entityRegistry.DestroyEntity(entityId);
            logger.Info(LoggedFeature.Networking, "Removed player entity {0} for peer {1}", entityId, peer.Id);

            // Remove the mapping
            _peerEntityMap.Remove(peer.Id);
        }
    }
}