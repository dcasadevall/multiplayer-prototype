using System;
using System.Collections.Generic;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Networking;

namespace Shared.Replication
{
    public interface IReplicationStats
    {
        /// <summary>
        /// Gets the time between deltas received from the server.
        /// </summary>
        TimeSpan TimeBetweenDeltas { get; }
    }

    /// <summary>
    /// Receives world state updates from the server and applies them to the local entity registry.
    /// 
    /// <para>
    /// This system is the client-side counterpart to the <see cref="ServerReplicationSystem"/>.
    /// It subscribes to <see cref="WorldDeltaMessage"/>s from the network, queues them, and
    /// applies the changes to the local <see cref="EntityRegistry"/> during its update tick.
    /// This keeps the client's world state synchronized with the server's authoritative state.
    /// </para>
    ///
    /// <para>
    /// It is assumed that this system is the first to run on the client each tick,
    /// ensuring other systems see the most up-to-date state.
    /// </para>
    /// </summary>
    [TickInterval(1)]
    public class ClientReplicationSystem : ISystem, IDisposable, IReplicationStats
    {
        public TimeSpan TimeBetweenDeltas { get; private set; } = TimeSpan.Zero;

        private readonly IDisposable _subscription;
        private readonly Queue<WorldDeltaMessage> _deltaMessages = new();
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
                ConsumeEntityDelta(registry, message.Deltas);
            }
        }

        private void HandleMessageReceived(int peerId, WorldDeltaMessage msg)
        {
            _deltaMessages.Enqueue(msg);
        }


        /// <summary>
        /// Applies a list of entity deltas to the local entity registry.
        /// This method processes each delta, creating, updating, or destroying entities and their components.
        /// </summary>
        /// <param name="registry">The entity registry to modify.</param>
        /// <param name="deltas">The list of changes to apply.</param>
        private static void ConsumeEntityDelta(EntityRegistry registry, List<EntityDelta> deltas)
        {
            foreach (var delta in deltas)
            {
                // If the entity is marked as destroyed, remove it from the registry
                if (delta.IsDestroyed)
                {
                    registry.DestroyEntity(new EntityId(delta.EntityId));
                    continue;
                }

                // If the entity is new, create it and add components
                var entity = registry.GetOrCreate(delta.EntityId);
                if (delta.IsNew)
                {
                    foreach (var component in delta.AddedOrModifiedComponents)
                    {
                        entity.AddComponent(component);
                    }

                    continue;
                }

                // If the entity already exists, update its components
                foreach (var componentType in delta.RemovedComponents)
                {
                    entity.Remove(componentType);
                }

                foreach (var component in delta.AddedOrModifiedComponents)
                {
                    entity.AddOrReplaceComponent(component);
                }
            }
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