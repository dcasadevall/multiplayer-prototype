using System;
using System.Collections;
using System.Collections.Generic;
using Shared.ECS.Entities;

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

        /// <summary>
        /// Creates a new entity with a unique ID and adds it to the world.
        /// </summary>
        /// <returns>The newly created <see cref="Entity"/>.</returns>
        public Entity CreateEntity()
        {
            var id = EntityId.New();
            var entity = new Entity(id);
            _entities.Add(id, entity);
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
        public void DestroyEntity(EntityId id) => _entities.Remove(id);

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
            return newEntity;
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
    }
}