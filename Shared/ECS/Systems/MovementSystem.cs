using System;
using System.Linq;
using Shared.ECS.Components;
using System.Numerics;
using Shared.ECS.Simulation;

namespace Shared.ECS.Systems
{
    /// <summary>
    /// System that handles entity movement based on velocity.
    /// Runs every tick to ensure smooth movement.
    /// </summary>
    [TickInterval(1)] // Run every tick
    public class MovementSystem : ISystem
    {
        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            // Get all entities with both position and velocity components
            var entities = entityRegistry.GetAll()
                .Where(e => e.Has<PositionComponent>() && e.Has<VelocityComponent>());

            foreach (var entity in entities)
            {
                if (entity.TryGet<PositionComponent>(out var position) &&
                    entity.TryGet<VelocityComponent>(out var velocity))
                {
                    // Update position based on velocity and delta time
                    position.Value += velocity.Value * deltaTime;
                }
            }
        }
    }
}