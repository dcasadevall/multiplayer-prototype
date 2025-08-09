using System;
using System.Collections.Generic;
using Shared.ECS.Entities;
using Shared.ECS.Components;
using Shared.ECS.Replication;
using System.Linq;

namespace Shared.ECS
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
        private readonly Dictionary<Guid, List<Type>> _addedOrModifiedComponents = new();
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

        private void HandleComponentAddedOrModified(Entity entity, Type componentType)
        {
            var entityId = entity.Id.Value;
            if (!_addedOrModifiedComponents.ContainsKey(entityId))
            {
                _addedOrModifiedComponents[entityId] = new List<Type>();
            }

            _addedOrModifiedComponents[entityId].Add(componentType);
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
            var allEntityIds = _createdEntities.Concat(_removedEntities).Distinct();

            foreach (var entityId in allEntityIds)
            {
                var isNew = _createdEntities.Contains(entityId);
                var delta = new EntityDelta
                {
                    EntityId = entityId,
                    IsNew = isNew,
                };

                if (isNew)
                {
                    var entity = _entities[new EntityId(entityId)];
                    delta.AddedOrModifiedComponents = entity.GetAllComponents().ToList();
                }

                if (_removedEntities.Contains(entityId) && _entities.TryGetValue(new EntityId(entityId), out var removedEntity))
                {
                    delta.RemovedComponents = removedEntity.GetAllComponents().Select(c => c.GetType()).ToList();
                }

                deltas.Add(delta);
            }

            _createdEntities.Clear();
            _removedEntities.Clear();

            return deltas;
        }

        #endregion
    }
}