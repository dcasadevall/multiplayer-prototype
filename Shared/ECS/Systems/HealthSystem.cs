using System;
using System.Linq;
using Shared.Damage;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Math;

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
            var entities = entityRegistry.With<HealthComponent>();

            foreach (var entity in entities)
            {
                var health = entity.GetRequired<HealthComponent>();

                // Regenerate health over time
                if (health.CurrentHealth < health.MaxHealth)
                {
                    var newHealth = Clamping.Min(health.CurrentHealth + HealthRegenRate, health.MaxHealth);
                    entity.AddOrReplaceComponent(new HealthComponent
                    {
                        MaxHealth = health.MaxHealth,
                        CurrentHealth = newHealth
                    });
                }
            }
        }
    }
}