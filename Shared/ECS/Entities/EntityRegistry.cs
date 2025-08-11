using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// Event triggered when a new entity is created.
        /// </summary>
        public event Action<Entity>? OnEntityCreated;

        /// <summary>
        /// Event triggered when an entity is destroyed.
        /// </summary>
        public event Action<Entity>? OnEntityDestroyed;

        private readonly Dictionary<EntityId, Entity> _entities = new();

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
        public Entity CreateEntity(EntityId entityId)
        {
            var entity = new Entity(entityId);
            _entities.Add(entityId, entity);
            OnEntityCreated?.Invoke(entity);
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
        /// Gets an entity by its ID.
        /// Assumes the entity exists and throws an exception if it does not.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public Entity Get(EntityId id)
        {
            if (TryGet(id, out var entity))
            {
                return entity;
            }

            throw new KeyNotFoundException($"Entity with ID {id} does not exist.");
        }

        /// <summary>
        /// Removes an entity from the world by its ID.
        /// </summary>
        /// <param name="id">The entity's unique identifier.</param>
        public void DestroyEntity(EntityId id)
        {
            if (_entities.TryGetValue(id, out var entity))
            {
                OnEntityDestroyed?.Invoke(entity);
                _entities.Remove(id);
            }
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
            return GetAll().Where(entity => entity.Has<T>());
        }

        /// <summary>
        /// WithAll returns all entities that contain all the specified component types.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public IEnumerable<Entity> WithAll<T, T1>() where T : IComponent where T1 : IComponent
        {
            return GetAll().Where(entity => entity.Has<T>() && entity.Has<T1>());
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
            return GetAll().Where(entity => entity.Has<T>() && entity.Has<T1>() && entity.Has<T2>());
        }
    }
}