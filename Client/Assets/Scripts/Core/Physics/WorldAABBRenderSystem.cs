using System.Collections.Generic;
using Core.ECS.Rendering;
using Shared.ECS;
using Shared.Physics;
using UnityEngine;

namespace Core.Physics
{
    /// <summary>
    /// This client-side system manages the visualization of <see cref="WorldAABBComponent"/> instances.
    /// It adds a <see cref="AABBVisualizer"/> to any entity's GameObject that has a WorldAABBComponent,
    /// and keeps its properties in sync. This is useful for debugging physics interactions in the Unity Editor.
    /// </summary>
    public class WorldAABBRenderSystem : ISystem
    {
        private readonly Dictionary<EntityId, AABBVisualizer> _visualizers = new();

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var boundedEntities = new List<EntityId>();
            foreach (var entity in registry.With<WorldAABBComponent>())
            {
                var entityId = entity.Id;
                boundedEntities.Add(entityId);

                if (!_visualizers.TryGetValue(entityId, out var visualizer))
                {
                    var go = new GameObject($"BoundingBox_{entityId}");
                    visualizer = go.AddComponent<AABBVisualizer>();
                    _visualizers[entityId] = visualizer;
                }
                
                var boundingBox = entity.GetRequired<WorldAABBComponent>();
                visualizer.Center = (boundingBox.Min + boundingBox.Max) / 2;
                visualizer.Size = boundingBox.Max - boundingBox.Min;
            }

            // Cleanup
            var toRemove = new List<EntityId>();
            foreach (var pair in _visualizers)
            {
                if (!boundedEntities.Contains(pair.Key))
                {
                    Object.Destroy(pair.Value.gameObject);
                    toRemove.Add(pair.Key);
                }
            }
            foreach (var id in toRemove)
            {
                _visualizers.Remove(id);
            }
        }
    }
}

