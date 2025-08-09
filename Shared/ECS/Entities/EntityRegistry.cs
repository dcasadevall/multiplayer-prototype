using System;
using System.Collections.Generic;
using System.Linq;
using Shared.ECS.Replication;

namespace Shared.ECS.Entities
{
    /// <summary>
    /// Manages the lifecycle, storage, and lookup of all entities in the ECS world.
    /// 
    /// <para>
    /// The <c>EntityRegistry</c> is responsible for:
    /// <list type="bullet">
    ///   <item>Creating new entities with unique IDs.</item>
    ///   <item>Storing and retrieving entities by their <see cref="EntityId"/>.</item>
    ///   <item>Destroying entities and removing them from the world.</item>
    ///   <item>Enumerating all entities for system processing.</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// Systems interact with the <c>EntityManager</c> to query and manipulate entities during simulation ticks.
    /// </para>
    /// </summary>
    public class EntityRegistry
    {
        private readonly Dictionary<EntityId, Entity> _entities = new();

        // The following fields track changes to entities for delta generation.
        // They are used to produce deltas for replication and synchronization.
        // We could technically decouple this from the EntityRegistry,
        // but for simplicity, we keep it here.
        private readonly List<EntityId> _createdEntities = new();
        private readonly List<EntityId> _removedEntities = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _addedComponents = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _modifiedComponents = new();
        private readonly Dictionary<EntityId, HashSet<IComponent>> _removedComponents = new();

        /// <summary>
        /// Creates a new entity with a unique ID and adds it to the world.
        /// </summary>
        /// <returns>The newly created <see cref="Entity"/>.</returns>
        public Entity CreateEntity()
        {
            return CreateEntity(EntityId.New());
        }

        /// <summary>
        /// Creates a new entity with the given ID and adds it to the world.
        /// </summary>
        /// <returns>The newly created <see cref="Entity"/>.</returns>
        private Entity CreateEntity(EntityId entityId)
        {
            var entity = new Entity(entityId);
            _entities.Add(entityId, entity);
            _createdEntities.Add(entityId);

            // Add for event handling
            entity.OnComponentAdded += HandleComponentAdded;
            entity.OnComponentModified += HandleComponentModified;
            entity.OnComponentRemoved += HandleComponentRemoved;

            return entity;
        }

        /// <summary>
        /// Attempts to retrieve an entity by its ID.
        /// </summary>
        /// <param name="id">The entity's unique identifier.</param>
        /// <param name="entity">The entity, if found.</param>
        /// <returns>True if the entity exists; otherwise, false.</returns>
        public bool TryGet(EntityId id, out Entity entity) => _entities.TryGetValue(id, out entity);

        /// <summary>
        /// Removes an entity from the world by its ID.
        /// </summary>
        /// <param name="id">The entity's unique identifier.</param>
        public void DestroyEntity(EntityId id)
        {
            // Clear event handlers
            _entities[id].OnComponentAdded -= HandleComponentAdded;
            _entities[id].OnComponentModified -= HandleComponentModified;
            _entities[id].OnComponentRemoved -= HandleComponentRemoved;

            // Remove the entity from the registry and track it as removed
            _entities.Remove(id);
            _removedEntities.Add(id);

            // Also remove any components that were added, modified, or removed in this delta
            _addedComponents.Remove(id);
            _modifiedComponents.Remove(id);
            _removedComponents.Remove(id);
        }

        /// <summary>
        /// Returns an enumerable of all entities currently in the world.
        /// </summary>
        public IEnumerable<Entity> GetAll() => _entities.Values;

        /// <summary>
        /// Attempts to retrieve an entity by its ID, or creates a new one if it does not exist.
        /// </summary>
        /// <param name="entityId">The ID to use for the entity.</param>
        /// <returns>The existing entity or a newly created one with the specified ID.</returns>
        public Entity GetOrCreate(Guid entityId)
        {
            var id = new EntityId(entityId);
            if (TryGet(id, out var entity))
            {
                return entity;
            }

            return CreateEntity(id);
        }

        /// <summary>
        /// WithAll returns all entities that contain the specified component type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<Entity> With<T>() where T : IComponent
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Has<T>())
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// WithAll returns all entities that contain all the specified component types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public IEnumerable<Entity> WithAll<T, T1>() where T : IComponent where T1 : IComponent
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Has<T>() && entity.Has<T1>())
                {
                    yield return entity;
                }
            }
        }

        /// <summary>
        /// WithAll returns all entities that contain all the specified component types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public IEnumerable<Entity> WithAll<T, T1, T2>() where T : IComponent where T1 : IComponent where T2 : IComponent
        {
            foreach (var entity in _entities.Values)
            {
                if (entity.Has<T>() && entity.Has<T1>() && entity.Has<T2>())
                {
                    yield return entity;
                }
            }
        }

        #region Entity Delta Handling

        private void HandleComponentAdded(Entity entity, IComponent component)
        {
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
            var entityId = entity.Id;
            if (!_modifiedComponents.ContainsKey(entityId))
            {
                _modifiedComponents[entityId] = new HashSet<IComponent>();
            }

            _modifiedComponents[entityId].Add(component);
        }

        private void HandleComponentRemoved(Entity entity, IComponent component)
        {
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

        /// <summary>
        /// Produces a list of <see cref="EntityDelta"/> objects representing the changes made to entities.
        /// Clears the tracked changes after producing the deltas.
        /// </summary>
        /// <returns></returns>
        public List<EntityDelta> ProduceEntityDelta()
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

                deltas.Add(new EntityDelta
                {
                    EntityId = entityId.Value,
                    IsNew = true,
                    AddedOrModifiedComponents = _entities[entityId].GetAllComponents().ToList()
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

                deltas.Add(new EntityDelta
                {
                    EntityId = entityId.Value,
                    AddedOrModifiedComponents = added.Concat(modified).ToList(),
                    RemovedComponents = _removedComponents
                        .GetValueOrDefault(entityId, new HashSet<IComponent>())
                        .Select(c => c.GetType())
                        .ToList()
                });
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
        /// Given a list of <see cref="EntityDelta"/> objects, applies the changes to the entity registry.
        /// This method processes each delta, adding or modifying components for new entities,
        /// and removing or modifying components for existing entities.
        ///
        /// For simplicity, wipe any tracked changes after consuming deltas.
        /// </summary>
        /// <param name="deltas"></param>
        public void ConsumeEntityDelta(List<EntityDelta> deltas)
        {
            foreach (var delta in deltas)
            {
                // If the entity is marked as destroyed, remove it from the registry
                if (delta.IsDestroyed)
                {
                    if (_entities.ContainsKey(new EntityId(delta.EntityId)))
                    {
                        DestroyEntity(new EntityId(delta.EntityId));
                    }

                    continue;
                }

                // If the entity is new, create it and add components
                if (delta.IsNew)
                {
                    var newEntity = GetOrCreate(delta.EntityId);
                    foreach (var component in delta.AddedOrModifiedComponents)
                    {
                        newEntity.AddComponent(component);
                    }

                    continue;
                }

                // If the entity already exists, update its components
                if (_entities.TryGetValue(new EntityId(delta.EntityId), out var existingEntity))
                {
                    foreach (var componentType in delta.RemovedComponents)
                    {
                        existingEntity.Remove(componentType);
                    }

                    foreach (var component in delta.AddedOrModifiedComponents)
                    {
                        existingEntity.AddOrReplaceComponent(component);
                    }
                }
            }

            // Clear the tracked changes after consuming deltas
            _addedComponents.Clear();
            _modifiedComponents.Clear();
            _removedComponents.Clear();
            _createdEntities.Clear();
            _removedEntities.Clear();
        }

        #endregion
    }
}