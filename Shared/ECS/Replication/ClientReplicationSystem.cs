using System;
using System.Collections.Generic;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Networking;

namespace Shared.ECS.Replication
{
    public interface IReplicationStats
    {
        /// <summary>
        /// Gets the time between deltas received from the server.
        /// </summary>
        TimeSpan TimeBetweenDeltas { get; }
    }

    /// <summary>
    /// ECS system responsible for consuming world deltas received from the server.
    /// 
    /// <para>
    /// This system acts as the client-side counterpart to the ServerReplicationSystem.
    /// It receives world deltas from the server and applies them to the local entity registry
    /// to keep the client's world state synchronized with the authoritative server state.
    /// </para>
    ///
    /// <para>
    /// One can assume that this system is always the first system to run on the client
    /// </para>
    /// </summary>
    [TickInterval(1)]
    public class ClientReplicationSystem : ISystem, IDisposable, IReplicationStats
    {
        public TimeSpan TimeBetweenDeltas { get; private set; } = TimeSpan.Zero;

        private readonly IDisposable _subscription;
        private Queue<WorldDeltaMessage> _deltaMessages = new Queue<WorldDeltaMessage>();
        private DateTime _lastUpdate = DateTime.MinValue;

        /// <summary>
        /// Constructs a new ClientReplicationSystem using dependency injection.
        /// </summary>
        /// <param name="messageReceiver">Receiver for network messages.</param>
        /// <param name="connection">Connection to the authoritative server.</param>
        public ClientReplicationSystem(IMessageReceiver messageReceiver, IClientConnection connection)
        {
            if (connection.InitialWorldSnapshot == null)
            {
                throw new ArgumentNullException(nameof(connection.InitialWorldSnapshot), "Initial world snapshot must not be null.");
            }

            _deltaMessages.Enqueue(connection.InitialWorldSnapshot);
            _subscription = messageReceiver.RegisterMessageHandler<WorldDeltaMessage>("ReplicationSystem", HandleMessageReceived);
        }

        /// <summary>
        /// Called by the world on each tick to process any pending network messages.
        /// </summary>
        /// <param name="registry">The entity registry containing all entities and components.</param>
        /// <param name="tickNumber">The current world tick number.</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            while (_deltaMessages.TryDequeue(out var message))
            {
                // Update the time between deltas
                var now = DateTime.UtcNow;
                if (_lastUpdate != DateTime.MinValue)
                {
                    TimeBetweenDeltas = now - _lastUpdate;
                }

                _lastUpdate = now;

                // Consume the world delta message
                registry.ConsumeEntityDelta(message.Deltas);
            }
        }

        private void HandleMessageReceived(int peerId, WorldDeltaMessage msg)
        {
            _deltaMessages.Enqueue(msg);
        }

        /// <summary>
        /// Cleanup method to unsubscribe from network events.
        /// </summary>
        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}