using System.Collections.Generic;
using Core.ECS.Rendering;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Entities;
using UnityEngine;

namespace Adapters.Health
{
    public class HealthBarRenderSystem : ISystem
    {
        private readonly Dictionary<EntityId, HealthBarView> _healthBars = new();
        private readonly IEntityViewRegistry _entityViewRegistry;
        private readonly GameObject _healthBarPrefab;

        public HealthBarRenderSystem(IEntityViewRegistry entityViewRegistry)
        {
            _entityViewRegistry = entityViewRegistry;
            _healthBarPrefab = Resources.Load<GameObject>("HealthBar");
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Find all players with health components
            foreach (var entity in registry.With<HealthComponent>())
            {
                var entityId = entity.Id;
                var health = entity.GetRequired<HealthComponent>();

                // If no health bar exists for this entity, create one
                if (!_healthBars.ContainsKey(entityId))
                {
                    if (_entityViewRegistry.TryGetEntityView(entityId, out var entityView))
                    {
                        var healthBarInstance = Object.Instantiate(_healthBarPrefab);
                        var healthBarDisplay = healthBarInstance.GetComponent<HealthBarView>();
                        healthBarDisplay.SetTarget(entityView);
                        _healthBars[entityId] = healthBarDisplay;
                    }
                }

                // Update the health bar value
                if (_healthBars.TryGetValue(entityId, out var display))
                {
                    display.UpdateHealth(health.CurrentHealth, health.MaxHealth);
                }
            }
            
            // Cleanup health bars for entities that no longer exist
            var toRemove = new List<EntityId>();
            foreach (var pair in _healthBars)
            {
                if (!registry.TryGet(pair.Key, out _))
                {
                    Object.Destroy(pair.Value.gameObject);
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var id in toRemove)
            {
                _healthBars.Remove(id);
            }
        }
    }
}
