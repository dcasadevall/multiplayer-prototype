using System.Collections.Generic;
using Core.ECS.Rendering;
using Shared.ECS;
using Shared.ECS.Entities;
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
        private readonly IEntityViewRegistry _entityViewRegistry;

        public WorldAABBRenderSystem(IEntityViewRegistry entityViewRegistry)
        {
            _entityViewRegistry = entityViewRegistry;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var boundedEntities = new List<EntityId>();
            foreach (var entity in registry.With<WorldAABBComponent>())
            {
                var entityId = entity.Id;
                boundedEntities.Add(entityId);
                
                // Get the view from the registry
                if (!_entityViewRegistry.TryGetEntityView(entityId, out var entityView))
                {
                    continue;
                }

                if (!entityView.TryGetComponent<AABBVisualizer>(out var visualizer))
                {
                    visualizer = entityView.gameObject.AddComponent<AABBVisualizer>();
                }
                
                var boundingBox = entity.GetRequired<WorldAABBComponent>();
                visualizer.Center = (boundingBox.Min + boundingBox.Max) / 2;
                visualizer.Size = boundingBox.Max - boundingBox.Min;
            }
        }
    }
}

