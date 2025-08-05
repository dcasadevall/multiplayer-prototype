using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.Logging;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Handles player spawn requests from clients.
    /// </summary>
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
            var x = Random.Shared.Next(-10, 10);
            var y = 0; // Ground level
            var z = Random.Shared.Next(-10, 10);

            try
            {
                // Create a new player entity
                var playerEntity = entityRegistry.CreateEntity();

                // Add position component.
                // This will be predicted by the client
                playerEntity.AddPredictedComponent(new PositionComponent
                {
                    X = x,
                    Y = y,
                    Z = z
                });

                // Add health component
                playerEntity.AddComponent(new HealthComponent
                {
                    MaxHealth = 100,
                    CurrentHealth = 100
                });

                // Add player tag for identification
                // playerEntity.AddTag("Player");

                // Add client ID component to link the entity to the client
                // Generate a random name for the player
                var name = $"Player_{peer.Id}";
                playerEntity.AddComponent(new PeerComponent
                {
                    PeerId = peer.Id,
                    PeerName = name
                });

                // Add a name component for display purposes
                playerEntity.AddComponent(new NameComponent
                {
                    Name = name,
                });

                // Add a prefab component to link to the player prefab
                // A hardcoded prefab name is fine for this example,
                // but in a real game we might want to load this dynamically
                // from a manifest or configuration file.
                playerEntity.AddComponent(new PrefabComponent
                {
                    PrefabName = "Player",
                });

                // Add a player tag component to identify this as a player entity
                playerEntity.AddComponent<PlayerTagComponent>();

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