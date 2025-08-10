using System;
using System.Collections.Generic;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Prediction;
using Shared.Scheduling;

namespace Shared.Replication
{
    /// <summary>
    /// Manages replication by tracking ECS changes and broadcasting them to clients.
    /// 
    /// <para>
    /// This system listens to events from the <see cref="EntityRegistry"/> to track
    /// created/destroyed entities and added/modified/removed components. On each eligible tick,
    /// it packages these changes into a <see cref="WorldDeltaMessage"/> and broadcasts it
    /// to all connected clients.
    /// </para>
    /// 
    /// <para>
    /// By centralizing replication logic here, the <see cref="EntityRegistry"/> remains a simple
    /// state container, and this system becomes the sole authority on what data is sent over
    /// the network and when. It also supports per-entity replication policies via the
    /// <see cref="PredictedComponent{T}"/> settings.
    /// </para>
    /// </summary>
    [TickInterval(1)]
    public class ServerReplicationSystem : ISystem, IInitializable, IDisposable
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly IMessageSender _messageSender;
        private readonly MessageFactory _messageFactory;
        private readonly ILogger _logger;

        // Delta tracking state
        private readonly List<EntityId> _createdEntities = new();
        private readonly List<EntityId> _removedEntities = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _addedComponents = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _modifiedComponents = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _removedComponents = new();

        /// <summary>
        /// Constructs a new <see cref="ServerReplicationSystem"/>.
        /// </summary>
        /// <param name="entityRegistry">The entity registry to monitor for changes.</param>
        /// <param name="messageSender">Sender used for sending network messages.</param>
        /// <param name="messageFactory">Factory for creating message instances.</param>
        /// <param name="logger">The logger for logging replication events.</param>
        public ServerReplicationSystem(EntityRegistry entityRegistry, IMessageSender messageSender, MessageFactory messageFactory,
            ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _messageSender = messageSender;
            _messageFactory = messageFactory;
            _logger = logger;
        }

        public void Initialize()
        {
            _entityRegistry.OnEntityCreated += HandleEntityCreated;
            _entityRegistry.OnEntityDestroyed += HandleEntityDestroyed;

            // Subscribe to events for existing entities
            foreach (var entity in _entityRegistry.GetAll())
            {
                entity.OnComponentAdded += HandleComponentAdded;
                entity.OnComponentModified += HandleComponentModified;
                entity.OnComponentRemoved += HandleComponentRemoved;
            }
        }

        public void Dispose()
        {
            _entityRegistry.OnEntityCreated -= HandleEntityCreated;
            _entityRegistry.OnEntityDestroyed -= HandleEntityDestroyed;

            foreach (var entity in _entityRegistry.GetAll())
            {
                entity.OnComponentAdded -= HandleComponentAdded;
                entity.OnComponentModified -= HandleComponentModified;
                entity.OnComponentRemoved -= HandleComponentRemoved;
            }
        }

        #region Entity Event Handlers

        private void HandleEntityCreated(Entity entity)
        {
            _createdEntities.Add(entity.Id);
            entity.OnComponentAdded += HandleComponentAdded;
            entity.OnComponentModified += HandleComponentModified;
            entity.OnComponentRemoved += HandleComponentRemoved;
        }

        private void HandleEntityDestroyed(Entity entity)
        {
            _removedEntities.Add(entity.Id);
            entity.OnComponentAdded -= HandleComponentAdded;
            entity.OnComponentModified -= HandleComponentModified;
            entity.OnComponentRemoved -= HandleComponentRemoved;

            // Clean up any tracked changes for the destroyed entity
            _addedComponents.Remove(entity.Id);
            _modifiedComponents.Remove(entity.Id);
            _removedComponents.Remove(entity.Id);
        }

        private void HandleComponentAdded(Entity entity, IComponent component)
        {
            // Skip server components, as they are not tracked for deltas
            if (component is INonReplicatedComponent)
            {
                return;
            }

            var entityId = entity.Id;
            if (!_addedComponents.ContainsKey(entityId))
            {
                _addedComponents[entityId] = new HashSet<IComponent>();
            }

            // If we are adding a component removed in this delta,
            // we should remove it from the removed list
            if (_removedComponents.ContainsKey(entityId))
            {
                if (_removedComponents[entityId].Contains(component))
                {
                    _removedComponents[entityId].Remove(component);
                }
            }

            _addedComponents[entityId].Add(component);
        }

        private void HandleComponentModified(Entity entity, IComponent component)
        {
            // Skip server components, as they are not tracked for deltas
            if (component is INonReplicatedComponent)
            {
                return;
            }

            var entityId = entity.Id;
            if (!_modifiedComponents.ContainsKey(entityId))
            {
                _modifiedComponents[entityId] = new HashSet<IComponent>();
            }

            _modifiedComponents[entityId].Add(component);
        }

        private void HandleComponentRemoved(Entity entity, IComponent component)
        {
            // Skip server components, as they are not tracked for deltas
            if (component is INonReplicatedComponent)
            {
                return;
            }

            var entityId = entity.Id;
            if (!_removedComponents.ContainsKey(entityId))
            {
                _removedComponents[entityId] = new HashSet<IComponent>();
            }

            // If we are removing a component that was added in this delta,
            // we should remove it from the added list
            if (_modifiedComponents.TryGetValue(entityId, out var modified) && modified.Contains(component))
            {
                modified.Remove(component);
            }

            // If we are removing a component that was added in this delta,
            // we should remove it from the added list
            if (_addedComponents.TryGetValue(entityId, out var added) && added.Contains(component))
            {
                added.Remove(component);
            }

            _removedComponents[entityId].Add(component);
        }

        #endregion

        /// <summary>
        /// Produces a list of <see cref="EntityDelta"/> objects representing the changes made to entities.
        /// Clears the tracked changes after producing the deltas.
        /// </summary>
        /// <returns></returns>
        private List<EntityDelta> ProduceEntityDelta(uint tickNumber)
        {
            var deltas = new List<EntityDelta>();

            // Handle created entities
            foreach (var entityId in _createdEntities)
            {
                if (_removedEntities.Contains(entityId))
                {
                    throw new InvalidOperationException(
                        $"Entity {entityId} cannot be created because it was previously destroyed.");
                }

                if (!_entityRegistry.TryGet(entityId, out var entity))
                {
                    throw new InvalidOperationException($"Entity {entityId} does not exist in the registry.");
                }

                var componentsToSend = new List<IComponent>();
                foreach (var component in entity.GetAllComponents())
                {
                    if (component is INonReplicatedComponent)
                    {
                        continue;
                    }

                    // If the component has a predicted counterpart, do not replicate the
                    // non-predicted component yet.
                    if (entity.HasPredictedComponent(component.GetType()))
                    {
                        continue;
                    }

                    if (component.IsPredicted())
                    {
                        if (component.ShouldBeReplicatedAtTick(0))
                        {
                            // Add the Predicted component
                            var p = (IPredictedComponent)component;
                            p.LastSentAtTick = tickNumber;
                            componentsToSend.Add(component);

                            // Now add the local counterpart
                            componentsToSend.Add(component.GetServerAuthoritativeValue());
                        }
                    }
                    else
                    {
                        componentsToSend.Add(component);
                    }
                }

                deltas.Add(new EntityDelta
                {
                    EntityId = entityId.Value,
                    IsNew = true,
                    AddedOrModifiedComponents = componentsToSend
                });
            }

            // Handle destroyed entities
            foreach (var entityId in _removedEntities)
            {
                if (_createdEntities.Contains(entityId))
                {
                    throw new InvalidOperationException(
                        $"Entity {entityId} cannot be destroyed because it was previously created.");
                }

                deltas.Add(new EntityDelta { EntityId = entityId.Value, IsDestroyed = true });
            }

            var modifiedAndRemoved = _addedComponents.Keys
                .Concat(_modifiedComponents.Keys)
                .Concat(_removedComponents.Keys)
                .Distinct();

            // Handle modified and removed components
            foreach (var entityId in modifiedAndRemoved)
            {
                if (_createdEntities.Contains(entityId) || _removedEntities.Contains(entityId)) continue;

                var added = _addedComponents.GetValueOrDefault(entityId, new HashSet<IComponent>());
                var modified = _modifiedComponents.GetValueOrDefault(entityId, new HashSet<IComponent>());

                if (!_entityRegistry.TryGet(entityId, out var entity))
                {
                    throw new InvalidOperationException($"Entity {entityId} does not exist in the registry.");
                }

                var componentsToSend = new List<IComponent>();
                foreach (var component in added.Concat(modified))
                {
                    if (component is INonReplicatedComponent)
                    {
                        continue;
                    }

                    // If the component has a predicted counterpart, do not replicate the
                    // non-predicted component.
                    if (entity.HasPredictedComponent(component.GetType()))
                    {
                        continue;
                    }

                    if (component.IsPredicted())
                    {
                        if (component.ShouldBeReplicatedAtTick(tickNumber))
                        {
                            var p = (IPredictedComponent)component;
                            p.LastSentAtTick = tickNumber;
                            componentsToSend.Add(component);
                        }
                    }
                    else
                    {
                        componentsToSend.Add(component);
                    }
                }

                // Prune local counterparts of predicted components so client can handle the removal of those
                var removed = _removedComponents.GetValueOrDefault(entityId, new HashSet<IComponent>());

                if (componentsToSend.Count > 0 || removed.Count > 0)
                {
                    deltas.Add(new EntityDelta
                    {
                        EntityId = entityId.Value,
                        AddedOrModifiedComponents = componentsToSend,
                        RemovedComponents = removed.Select(c => c.GetType()).ToList()
                    });
                }
            }

            // Remove the tracked changes after producing deltas
            _addedComponents.Clear();
            _modifiedComponents.Clear();
            _removedComponents.Clear();
            _createdEntities.Clear();
            _removedEntities.Clear();

            return deltas;
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
            deltaMessage.Deltas = ProduceEntityDelta(tickNumber);

            if (deltaMessage.Deltas.Count > 0)
            {
                _logger.Debug(LoggedFeature.Replication,
                    "Broadcasting replication delta for tick {0} with {1} entities.",
                    tickNumber, deltaMessage.Deltas.Count);

                _messageSender.BroadcastMessage(MessageType.Delta, deltaMessage, ChannelType.ReliableOrdered);
            }
        }
    }
}