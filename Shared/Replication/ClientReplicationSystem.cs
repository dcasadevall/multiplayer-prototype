using System;
using System.Collections.Generic;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Networking;
using Shared.Prediction;

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

                // Added or modified components that are not the local counterpart of a predicted component
                var addedOrModifiedComponents = delta.AddedOrModifiedComponents
                    .Where(x =>
                        !entity.Has(PredictedComponentExtensions.GetLocalPredictedCounterpartType(x.GetType())));

                if (delta.IsNew)
                {
                    foreach (var component in addedOrModifiedComponents)
                    {
                        // If the component is a PredictedComponent<> wrapper
                        // We set the server authoritative value
                        // and add the local component counterpart
                        // We assume the server only sends this component
                        // based on the tick replication mode.
                        if (component.IsPredicted())
                        {
                            entity.AddPredictedComponent(component);
                        }

                        if (entity.TrySetServerAuthoritativeValue(component.GetType(), component))
                        {
                        }
                        else
                        {
                            entity.AddComponent(component);
                        }
                    }

                    continue;
                }

                // If the entity already exists, update its components
                foreach (var componentType in delta.RemovedComponents)
                {
                    // If a predicted component is removed, we also remove its local counterpart
                    if (PredictedComponentExtensions.IsPredicted(componentType))
                    {
                        // Get the local counterpart of the predicted component
                        var localCounterpartType = PredictedComponentExtensions.GetLocalPredictedCounterpartType(componentType);
                        if (entity.TryGet(localCounterpartType, out var _))
                        {
                            entity.Remove(localCounterpartType);
                        }
                    }

                    entity.Remove(componentType);
                }

                foreach (var component in delta.AddedOrModifiedComponents)
                {
                    if (component.IsPredicted())
                    {
                        // Update the ServerValue of the existing PredictedComponent
                        // The local counterpart will be sent as a separate modified component
                        // so we don't need to do anything here
                        entity.SetServerValue(((IPredictedComponent)component).GetServerValue());
                    }
                    else
                    {
                        entity.AddOrReplaceComponent(component);
                    }
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