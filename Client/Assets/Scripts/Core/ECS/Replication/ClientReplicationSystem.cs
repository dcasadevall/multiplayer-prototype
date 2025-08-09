using System;
using Shared.ECS;
using Shared.ECS.Replication;
using Shared.ECS.Simulation;
using Shared.Networking;

namespace Core.ECS.Replication
{
    /// <summary>
    /// ECS system responsible for consuming world deltas received from the server.
    /// 
    /// <para>
    /// This system acts as the client-side counterpart to the server's ReplicationSystem.
    /// It receives world deltas from the server and applies them to the local entity registry
    /// to keep the client's world state synchronized with the authoritative server state.
    /// </para>
    /// 
    /// <para>
    /// The ClientReplicationSystem manages an IWorldDeltaConsumer, which deserializes
    /// incoming deltas and updates the local entity registry with the latest server state.
    /// </para>
    ///
    /// <para>
    /// One can assume that this system is always the first system to run on the client
    /// </para>
    /// </summary>
    [TickInterval(1)] // Process deltas as frequently as possible
    public class ClientReplicationSystem : ISystem, IDisposable
    {
        private readonly IWorldDeltaConsumer _worldDeltaConsumer;
        private readonly IDisposable _subscription;
        private DateTime _lastDeltaTime;

        /// <summary>
        /// Constructs a new ClientReplicationSystem using dependency injection.
        /// </summary>
        /// <param name="worldDeltaConsumer">Consumer used for processing incoming deltas.</param>
        /// <param name="messageReceiver">Receiver for network messages.</param>
        public ClientReplicationSystem(IWorldDeltaConsumer worldDeltaConsumer, IMessageReceiver messageReceiver)
        {
            _worldDeltaConsumer = worldDeltaConsumer;
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
        }

        private void HandleMessageReceived(int peerId, WorldDeltaMessage msg)
        {
            if (_lastDeltaTime != default)
            {
            }
            _worldDeltaConsumer.ConsumeDelta(msg);
            _lastDeltaTime = DateTime.Now;
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