using System;
using System.Linq;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using Shared.Health;

namespace Shared.ECS.Systems
{
    /// <summary>
    /// System that handles health regeneration and status effects.
    /// Runs every 10 ticks (10 times per second at 30Hz) since health changes
    /// don't need to be as frequent as movement.
    /// </summary>
    [TickInterval(10)] // Run every 10th tick
    public class HealthSystem : ISystem
    {
        private const int HealthRegenRate = 5; // Health per 10 ticks

        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            // Get all entities with health components
            var entities = entityRegistry.GetAll()
                .Where(e => e.Has<HealthComponent>());

            foreach (var entity in entities)
            {
                if (entity.TryGet<HealthComponent>(out var health))
                {
                    // Regenerate health over time
                    if (health.CurrentHealth < health.MaxHealth)
                    {
                        health.CurrentHealth += HealthRegenRate;

                        // Clamp to max health
                        if (health.CurrentHealth > health.MaxHealth)
                        {
                            health.CurrentHealth = health.MaxHealth;
                        }
                    }
                }
            }
        }
    }
}