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
    /// This system uses a delta-based replication approach for simplicity and reliability. Since the world tick is
    /// guaranteed to be sequential and deterministic, we can safely broadcast the delta state of all replicated entities
    /// at regular intervals. This ensures all clients remain synchronized with the authoritative server state.
    /// </para>
    /// 
    /// <para>
    /// The <see cref="ReplicationSystem"/> manages a <see cref="IWorldDeltaProducer"/>, which serializes
    /// all entities marked with <c>ReplicatedEntityComponent</c> and their <c>ISerializableComponent</c> data.
    /// Deltas are sent to all connected peers using reliable, ordered delivery.
    /// </para>
    /// </summary>
    // NOTE: It's okay to replicate every tick, but in a real game we would likely want to reduce this frequency
    // to avoid flooding the network with large deltas.
    // We would send deltas and chunk large deltas.
    [TickInterval(1)]
    public class ReplicationSystem : ISystem
    {
        private readonly IMessageSender _messageSender;

        /// <summary>
        /// Constructs a new <see cref="ReplicationSystem"/> for the given network manager.
        /// </summary>
        /// <param name="messageSender">Sender used for sending network messages.</param>
        public ReplicationSystem(IMessageSender messageSender)
        {
            _messageSender = messageSender;
        }

        /// <summary>
        /// Called by the world on each eligible tick to replicate the current state to all clients.
        /// Sends a delta of the world state to all connected peers.
        /// </summary>
        /// <param name="registry">The entity registry containing all entities and components.</param>
        /// <param name="tickNumber">The current world tick number (sequential and deterministic).</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var delta = new WorldDeltaMessage
            {
                Deltas = registry.ProduceEntityDelta()
            };

            if (delta.Deltas.Count > 0)
            {
                _messageSender.BroadcastMessage(MessageType.Delta, delta, ChannelType.ReliableOrdered);
            }
        }
    }
}