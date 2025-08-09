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
        private readonly List<Guid> _createdEntities = new();
        private readonly List<Guid> _removedEntities = new();
        private readonly Dictionary<Guid, List<IComponent>> _addedOrModifiedComponents = new();
        private readonly Dictionary<Guid, List<Type>> _removedComponents = new();

        /// <summary>
        /// Creates a new entity with a unique ID and adds it to the world.
        /// </summary>
        /// <returns>The newly created <see cref="Entity"/>.</returns>
        public Entity CreateEntity()
        {
            var id = EntityId.New();
            var entity = new Entity(id);
            _entities.Add(id, entity);
            _createdEntities.Add(id.Value);

            // Add for event handling
            entity.OnComponentUpdated += HandleComponentAddedOrModified;
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
            _entities[id].OnComponentUpdated -= HandleComponentAddedOrModified;
            _entities[id].OnComponentRemoved -= HandleComponentRemoved;

            // Remove the entity from the registry and track it as removed
            _entities.Remove(id);
            _removedEntities.Add(id.Value);
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

            var newEntity = new Entity(id);
            _entities.Add(id, newEntity);
            _createdEntities.Add(id.Value);
            newEntity.OnComponentUpdated += HandleComponentAddedOrModified;
            newEntity.OnComponentRemoved += HandleComponentRemoved;
            return newEntity;
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

        private void HandleComponentAddedOrModified(Entity entity, IComponent component)
        {
            var entityId = entity.Id.Value;
            if (!_addedOrModifiedComponents.ContainsKey(entityId))
            {
                _addedOrModifiedComponents[entityId] = new List<IComponent>();
            }

            _addedOrModifiedComponents[entityId].Add(component);
        }

        private void HandleComponentRemoved(Entity entity, Type componentType)
        {
            var entityId = entity.Id.Value;
            if (!_removedComponents.ContainsKey(entityId))
            {
                _removedComponents[entityId] = new List<Type>();
            }

            _removedComponents[entityId].Add(componentType);
        }

        /// <summary>
        /// Produces a list of <see cref="EntityDelta"/> objects representing the changes made to entities.
        /// Clears the tracked changes after producing the deltas.
        /// </summary>
        /// <returns></returns>
        public List<EntityDelta> ProduceEntityDelta()
        {
            var deltas = new List<EntityDelta>();
            var allEntityIds = _createdEntities
                .Concat(_removedEntities)
                .Concat(_addedOrModifiedComponents.Keys)
                .Concat(_removedComponents.Keys)
                .Distinct();

            foreach (var entityId in allEntityIds)
            {
                var isNew = _createdEntities.Contains(entityId);
                var isDestroyed = _removedEntities.Contains(entityId);

                if (isNew && isDestroyed)
                {
                    throw new Exception($"Entity {entityId} Marked as both new and destroyed");
                }

                // IsDestroyed entities should not be processed further
                if (isDestroyed)
                {
                    deltas.Add(new EntityDelta
                    {
                        EntityId = entityId,
                        IsDestroyed = true,
                    });
                    continue;
                }

                var delta = new EntityDelta
                {
                    EntityId = entityId,
                    IsNew = isNew,
                    AddedOrModifiedComponents = _addedOrModifiedComponents.GetValueOrDefault(entityId, new List<IComponent>()),
                    RemovedComponents = _removedComponents.GetValueOrDefault(entityId, new List<Type>())
                };

                deltas.Add(delta);
            }

            // Remove the tracked changes after producing deltas
            _addedOrModifiedComponents.Clear();
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
                    DestroyEntity(new EntityId(delta.EntityId));
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
            _addedOrModifiedComponents.Clear();
            _removedComponents.Clear();
            _createdEntities.Clear();
            _removedEntities.Clear();
        }

        #endregion
    }
}