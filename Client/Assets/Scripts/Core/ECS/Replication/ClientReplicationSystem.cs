using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Networking;
using Shared.Networking.Replication;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// ECS system responsible for consuming world snapshots received from the server.
    /// 
    /// <para>
    /// This system acts as the client-side counterpart to the server's ReplicationSystem.
    /// It receives world snapshots from the server and applies them to the local entity registry
    /// to keep the client's world state synchronized with the authoritative server state.
    /// </para>
    /// 
    /// <para>
    /// The ClientReplicationSystem manages an IWorldSnapshotConsumer, which deserializes
    /// incoming snapshots and updates the local entity registry with the latest server state.
    /// </para>
    ///
    /// <para>
    /// One can assume that this system is always the first system to run on the client
    /// </para>
    /// </summary>
    [TickInterval(1)] // Process snapshots as frequently as possible
    public class ClientReplicationSystem : ISystem
    {
        private readonly IWorldSnapshotConsumer _worldSnapshotConsumer;
        private readonly IMessageReceiver _messageReceiver;

        /// <summary>
        /// Constructs a new ClientReplicationSystem using dependency injection.
        /// </summary>
        /// <param name="worldSnapshotConsumer">Consumer used for processing incoming snapshots.</param>
        /// <param name="messageReceiver">Receiver for network messages.</param>
        public ClientReplicationSystem(IWorldSnapshotConsumer worldSnapshotConsumer, 
            IMessageReceiver messageReceiver)
        {
            _worldSnapshotConsumer = worldSnapshotConsumer;
            _messageReceiver = messageReceiver;
            
            // Subscribe to snapshot messages
            _messageReceiver.OnMessageReceived += HandleMessageReceived;
        }

        /// <summary>
        /// Called by the world on each tick to process any pending network messages.
        /// </summary>
        /// <param name="registry">The entity registry containing all entities and components.</param>
        /// <param name="tickNumber">The current world tick number.</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // The actual snapshot processing happens in HandleMessageReceived
            // This method is called every tick to ensure we process messages promptly
        }

        private void HandleMessageReceived(MessageType messageType, byte[] data)
        {
            if (messageType == MessageType.Snapshot)
            {
                _worldSnapshotConsumer.ConsumeSnapshot(data);
            }
        }

        /// <summary>
        /// Cleanup method to unsubscribe from network events.
        /// </summary>
        public void Dispose()
        {
            if (_messageReceiver != null)
            {
                _messageReceiver.OnMessageReceived -= HandleMessageReceived;
            }
        }
    }
} 