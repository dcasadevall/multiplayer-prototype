using System;
using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Logging;
using Shared.Physics;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;
using Object = UnityEngine.Object;
using Vector3 = System.Numerics.Vector3;

namespace Core.ECS.Rendering
{
    /// <summary>
    /// ECS system responsible for rendering entities as Unity GameObjects.
    /// 
    /// <para>
    /// This system bridges the gap between the ECS world and Unity's rendering system.
    /// It creates, updates, and destroys Unity GameObjects based on the entities in the
    /// ECS world, ensuring that the visual representation stays in sync with the game logic.
    /// </para>
    ///
    /// <para>
    /// It should be assumed that this system runs after the replication system,
    /// but before any other systems that might modify entity components.
    /// </para>
    /// </summary>
    [TickInterval(1)] // Update every frame
    public class EntityViewSystem : ISystem, IEntityViewRegistry, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Dictionary<EntityId, GameObject> _entityViews = new();
        private readonly Transform _worldRoot;
        
        /// <summary>
        /// Constructs a new EntityViewSystem.
        /// </summary>
        public EntityViewSystem(ILogger logger)
        {
            _logger = logger;
            _worldRoot = new GameObject("ECS World Root").transform;
        }

        /// <inheritdoc />
        public bool TryGetEntityView(EntityId entityId, out Transform view)
        {
            if (_entityViews.TryGetValue(entityId, out var gameObject))
            {
                view = gameObject.transform;
                return view;
            }
            
            view = null;
            return false;
        }
        
        /// <inheritdoc />
        public Transform GetEntityView(EntityId entityId)
        {
            return _entityViews[entityId]?.transform;
        }
        
        /// <summary>
        /// Called by the world on each tick to update entity views.
        /// </summary>
        /// <param name="registry">The entity registry containing all entities and components.</param>
        /// <param name="tickNumber">The current world tick number.</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Process all entities with position components
            foreach (var entity in registry.GetAll())
            {
                if (entity.Has<PositionComponent>())
                {
                    UpdateEntityView(registry, entity);
                }
            }
            
            // Clean up views for entities that no longer exist
            CleanupOrphanedViews(registry);
        }

        /// <summary>
        /// Updates the Unity GameObject for a given entity.
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="entity">The entity to update.</param>
        private void UpdateEntityView(EntityRegistry registry, Entity entity)
        {
            var entityId = entity.Id;
            
            // Create view if it doesn't exist
            if (!_entityViews.ContainsKey(entityId))
            {
                CreateEntityView(entity);
            }
            
            // Destroy any local counterpart if the entity
            // has SpawnAuthority
            TryDestroyLocalEntityView(registry, entity);
            
            // Update the view's position
            if (_entityViews.TryGetValue(entityId, out var view))
            {
                var positionComponent = entity.Get<PositionComponent>();
                view.transform.position = (positionComponent?.Value ?? Vector3.Zero).ToUnityVector3();

                if(entity.TryGet<RotationComponent>(out var rotationComponent))
                    view.transform.rotation = rotationComponent.Value.ToUnityQuaternion();
            }
        }
        
        /// <summary>
        /// Creates a Unity GameObject for a given entity.
        /// </summary>
        /// <param name="entity">The entity to create a view for.</param>
        private void CreateEntityView(Entity entity)
        {
            var entityId = entity.Id;

            // Load the prefab or create a default primitive
            GameObject view;
            if (entity.TryGet<PrefabComponent>(out var prefabComponent))
            {
                // Resources.Load is fine for this sample, but in a real game
                // we might want to use a more robust asset management system.
                // We also could use pooling here for performance.
                var prefab = Resources.Load<GameObject>(prefabComponent.PrefabName);
                view = Object.Instantiate(prefab);
            }
            else
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

#if UNITY_EDITOR
            view.name = $"Entity_{entityId}";

            if (entity.TryGet<NameComponent>(out var nameComponent))
            {
                view.name = nameComponent.Name;
            }
#endif
            view.transform.SetParent(_worldRoot);
            
            // Set initial position
            if (entity.Has<PositionComponent>())
            {
                var positionComponent = entity.Get<PositionComponent>();
                view.transform.position = (positionComponent?.Value ?? Vector3.Zero).ToUnityVector3();
            }

            if (entity.TryGet<RotationComponent>(out var rotationComponent))
            {
                view.transform.rotation = rotationComponent.Value.ToUnityQuaternion();
            }

            // Store the view
            _entityViews[entityId] = view;
            
            Debug.Log($"EntityViewSystem: Created view for entity {entityId}");
        }

        /// <summary>
        /// Trys to locate the local counterpart of a remotely spawned entity
        /// and destroys it if it exists.
        /// </summary>
        /// <param name="entityRegistry"></param>
        /// <param name="entity"></param>
        private void TryDestroyLocalEntityView(EntityRegistry entityRegistry, Entity entity)
        {
            // Does this entity have a SpawnAuthorityComponent?
            if (!entity.TryGet<SpawnAuthorityComponent>(out var spawnAuthorityComponent))
            {
                return;
            }
            
            // If it does, we need to check if the local entity exists
            if (!entityRegistry.TryGet(new EntityId(spawnAuthorityComponent.LocalEntityId), out var localMatchedEntity))
            {
                return;
            }
            
            // If it does, we need to destroy the local counterpart
            if (_entityViews.TryGetValue(localMatchedEntity.Id, out var localView))
            {
                // We do not destroy the entity itself, just the view
                // This system will destroy entities without an associated view
                // Object.Destroy(localView);
                // _entityViews.Remove(localMatchedEntity.Id);
                entityRegistry.DestroyEntity(localMatchedEntity.Id);
                _logger.Debug(LoggedFeature.Replication, 
                    $"EntityViewSystem: Destroyed local view for entity {localMatchedEntity.Id}");
            }
            else
            {
                _logger.Warn(LoggedFeature.Ecs, $"EntityViewSystem: No local view found for entity {localMatchedEntity.Id}");
            }
        }
        
        /// <summary>
        /// Removes views for entities that no longer exist in the registry.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        private void CleanupOrphanedViews(EntityRegistry registry)
        {
            var existingEntityIds = new HashSet<EntityId>();
            foreach (var entity in registry.GetAll())
            {
                existingEntityIds.Add(entity.Id);
            }
            
            var orphanedViews = new List<EntityId>();
            foreach (var kvp in _entityViews)
            {
                if (!existingEntityIds.Contains(kvp.Key))
                {
                    orphanedViews.Add(kvp.Key);
                }
            }
            
            foreach (var orphanedId in orphanedViews)
            {
                if (_entityViews.TryGetValue(orphanedId, out var view))
                {
                    Object.Destroy(view);
                    _entityViews.Remove(orphanedId);
                    Debug.Log($"EntityViewSystem: Removed view for entity {orphanedId}");
                }
            }
        }
        
        /// <summary>
        /// Cleans up all entity views.
        /// </summary>
        public void Dispose()
        {
            foreach (var view in _entityViews.Values)
            {
                if (view != null)
                {
                    Object.Destroy(view);
                }
            }
            _entityViews.Clear();
            
            if (_worldRoot != null)
            {
                Object.Destroy(_worldRoot.gameObject);
            }
        }
    }
} 