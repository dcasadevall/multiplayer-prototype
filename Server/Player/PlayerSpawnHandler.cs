using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
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
                // Create a new player entity
                var playerEntity = entityRegistry.CreateEntity();

                playerEntity.AddPredictedComponent(new PositionComponent { X = x, Y = y, Z = z });
                playerEntity.AddPredictedComponent(new VelocityComponent());

                playerEntity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 100 });

                // Add client ID component to link the entity to the client
                // Generate a random name for the player
                var name = $"Player_{peer.Id}";
                playerEntity.AddComponent(new PeerComponent { PeerId = peer.Id, PeerName = name });
                playerEntity.AddComponent(new NameComponent { Name = name });
                playerEntity.AddComponent(new PrefabComponent { PrefabName = "Player" });
                playerEntity.AddComponent<PlayerTagComponent>();
                playerEntity.AddComponent(new RotationComponent());

                // Mark as replicated so it gets sent to clients
                playerEntity.AddComponent<ReplicatedTagComponent>();

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