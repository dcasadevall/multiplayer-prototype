using System.Linq;
using Core.ECS.Rendering;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Entities;
using UnityEngine;

namespace Adapters.Rendering
{
    /// <summary>
    /// Client-side system that makes invulnerable entities "blink" until their InvulnerableComponent expires.
    /// This is a very naive approach but works for this sample.
    /// </summary>
    public class InvulnerableEntityRenderingSystem : ISystem
    {
        private const float BlinkHz = 5f; // 5 times per second
        private readonly IEntityViewRegistry _entityViewRegistry;

        public InvulnerableEntityRenderingSystem(IEntityViewRegistry entityViewRegistry)
        {
            _entityViewRegistry = entityViewRegistry;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var entities = registry.With<InvulnerableComponent>().ToList();
            foreach (var entity in entities)
            {
                if (!_entityViewRegistry.TryGetEntityView(entity.Id, out var transform)) continue;

                var renderers = transform.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    // Blink by toggling enabled state
                    var t = Time.time * BlinkHz;
                    var on = (Mathf.FloorToInt(t) % 2) == 0;
                    renderer.enabled = on;
                }
            }
            
            entities = registry.Without<InvulnerableComponent>().ToList();
            foreach (var entity in entities)
            {
                if (!_entityViewRegistry.TryGetEntityView(entity.Id, out var transform)) continue;

                var renderers = transform.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
            }
        }
    }
}


