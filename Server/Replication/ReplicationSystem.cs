using Shared.ECS;
using Shared.ECS.Replication;
using Shared.ECS.Simulation;
using Shared.Networking;
using Shared.Networking.Messages;

namespace Server.Replication
{
    /// <summary>
    /// ECS system responsible for replicating the current world state to all connected clients on a fixed interval.
    /// 
    /// <para>
    /// This system uses a snapshot-based replication approach for simplicity and reliability. Since the world tick is
    /// guaranteed to be sequential and deterministic, we can safely broadcast the full state of all replicated entities
    /// at regular intervals. This ensures all clients remain synchronized with the authoritative server state.
    /// </para>
    /// 
    /// <para>
    /// The <see cref="ReplicationSystem"/> manages a <see cref="IWorldSnapshotProducer"/>, which serializes
    /// all entities marked with <c>ReplicatedEntityComponent</c> and their <c>ISerializableComponent</c> data.
    /// Snapshots are sent to all connected peers using reliable, ordered delivery.
    /// </para>
    /// </summary>
    // NOTE: It's okay to replicate every tick, but in a real game we would likely want to reduce this frequency
    // to avoid flooding the network with large snapshots.
    // We would send deltas and chunk large snapshots.
    [TickInterval(1)]
    public class ReplicationSystem : ISystem
    {
        private readonly IMessageSender _messageSender;
        private readonly IWorldSnapshotProducer _worldSnapshotProducer;

        /// <summary>
        /// Constructs a new <see cref="ReplicationSystem"/> for the given network manager.
        /// </summary>
        /// <param name="messageSender">Sender used for sending network messages.</param>
        /// <param name="worldSnapshotProducer"></param>
        public ReplicationSystem(IMessageSender messageSender, IWorldSnapshotProducer worldSnapshotProducer)
        {
            _messageSender = messageSender;
            _worldSnapshotProducer = worldSnapshotProducer;
        }

        /// <summary>
        /// Called by the world on each eligible tick to replicate the current state to all clients.
        /// Sends a full snapshot to all connected peers.
        /// </summary>
        /// <param name="registry">The entity registry containing all entities and components.</param>
        /// <param name="tickNumber">The current world tick number (sequential and deterministic).</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // _logger.Debug("ReplicationSystem: Sending snapshot to all clients...");
            var snapshot = _worldSnapshotProducer.ProduceSnapshot();

            // Broadcast the snapshot to all connected clients
            // We use the unreliable channel for snapshot delivery
            // since the snapshot is a full state for simplicity.

            // TODO: We should be sending delta updates and use the unreliable channel.
            // we should be chunking anything over 1kb
            _messageSender.BroadcastMessage(MessageType.Snapshot, snapshot, ChannelType.ReliableOrdered);
        }
    }
}