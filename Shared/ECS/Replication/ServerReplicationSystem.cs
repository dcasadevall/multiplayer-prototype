using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;

namespace Shared.ECS.Replication
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
    /// The <see cref="ServerReplicationSystem"/> uses the <see cref="EntityRegistry"/> ProduceEntityDelta method, which serializes
    /// all entities marked with <c>ReplicatedEntityComponent</c> and their <c>ISerializableComponent</c> data.
    /// Deltas are sent to all connected peers using reliable, ordered delivery.
    /// </para>
    /// </summary>
    [TickInterval(1)]
    public class ServerReplicationSystem : ISystem
    {
        private readonly IMessageSender _messageSender;
        private readonly MessageFactory _messageFactory;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructs a new <see cref="ServerReplicationSystem"/> for the given network manager.
        /// </summary>
        /// <param name="messageSender">Sender used for sending network messages.</param>
        /// <param name="messageFactory">Factory for creating message instances.</param>
        /// <param name="logger">The logger for logging replication events.</param>
        public ServerReplicationSystem(IMessageSender messageSender, MessageFactory messageFactory, ILogger logger)
        {
            _messageSender = messageSender;
            _messageFactory = messageFactory;
            _logger = logger;
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
            var deltaMessage = (WorldDeltaMessage)_messageFactory.Create(MessageType.Delta);
            deltaMessage.Deltas = registry.ProduceEntityDelta();

            if (deltaMessage.Deltas.Count > 0)
            {
                _logger.Debug(LoggedFeature.Replication,
                    "Broadcasting replication delta for tick {0} with {1} entities",
                    tickNumber, deltaMessage.Deltas.Count);

                _messageSender.BroadcastMessage(MessageType.Delta, deltaMessage, ChannelType.ReliableOrdered);
            }
        }
    }
}