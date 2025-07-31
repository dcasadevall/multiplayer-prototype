using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Core
{
    /// <summary>
    /// ECS system responsible for rendering entities as Unity GameObjects.
    /// 
    /// <para>
    /// This system bridges the gap between the ECS world and Unity's rendering system.
    /// It creates, updates, and destroys Unity GameObjects based on the entities in the
    /// ECS world, ensuring that the visual representation stays in sync with the game logic.
    /// </para>
    /// </summary>
    [TickInterval(1)] // Update every frame
    public class EntityViewSystem : ISystem
    {
        private readonly Dictionary<EntityId, GameObject> _entityViews = new();
        private readonly Transform _worldRoot;
        
        /// <summary>
        /// Constructs a new EntityViewSystem using dependency injection.
        /// </summary>
        /// <param name="unityBehaviour">Unity MonoBehaviour for accessing Unity APIs.</param>
        public EntityViewSystem(MonoBehaviour unityBehaviour)
        {
            _worldRoot = new GameObject("ECS World Root").transform;
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
                    UpdateEntityView(entity, registry);
                }
            }
            
            // Clean up views for entities that no longer exist
            CleanupOrphanedViews(registry);
        }
        
        /// <summary>
        /// Updates the Unity GameObject for a given entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="registry">The entity registry.</param>
        private void UpdateEntityView(Entity entity, EntityRegistry registry)
        {
            var entityId = entity.Id;
            
            // Create view if it doesn't exist
            if (!_entityViews.ContainsKey(entityId))
            {
                CreateEntityView(entity, registry);
            }
            
            // Update the view's position
            if (_entityViews.TryGetValue(entityId, out var view))
            {
                var positionComponent = entity.Get<PositionComponent>();
                view.transform.position = (positionComponent?.Value ?? Vector3.Zero).ToUnityVector3();
                
                // Update velocity if present
                if (entity.Has<VelocityComponent>())
                {
                    var velocityComponent = entity.Get<VelocityComponent>();
                    // You could add visual effects based on velocity here
                    // For example, particle trails, rotation, etc.
                }
            }
        }
        
        /// <summary>
        /// Creates a Unity GameObject for a given entity.
        /// </summary>
        /// <param name="entity">The entity to create a view for.</param>
        /// <param name="registry">The entity registry.</param>
        private void CreateEntityView(Entity entity, EntityRegistry registry)
        {
            var entityId = entity.Id;
            
            // Create a simple GameObject for now
            // In a real implementation, you might load prefabs based on entity tags or components
            var view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = $"Entity_{entityId}";
            view.transform.SetParent(_worldRoot);
            
            // Set initial position
            if (entity.Has<PositionComponent>())
            {
                var positionComponent = entity.Get<PositionComponent>();
                view.transform.position = (positionComponent?.Value ?? Vector3.Zero).ToUnityVector3();
            }
            
            // Store the view
            _entityViews[entityId] = view;
            
            Debug.Log($"EntityViewSystem: Created view for entity {entityId}");
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
        /// Gets the Unity GameObject for a given entity ID.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <returns>The Unity GameObject, or null if not found.</returns>
        public GameObject GetEntityView(EntityId entityId)
        {
            return _entityViews.TryGetValue(entityId, out var view) ? view : null;
        }
        
        /// <summary>
        /// Cleans up all entity views.
        /// </summary>
        public void Cleanup()
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