using System;
using System.Linq;
using Shared.ECS.Components;
using System.Numerics;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Physics;

namespace Shared.ECS.Systems
{
    /// <summary>
    /// System that handles entity movement based on velocity.
    /// Runs every tick to ensure smooth movement.
    /// </summary>
    [TickInterval(1)] // Run every tick
    public class VelocitySystem : ISystem
    {
        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            // Get all entities with both position and velocity components
            var entities = entityRegistry
                .WithAll<PositionComponent, VelocityComponent>()
                .ToList();

            foreach (var entity in entities)
            {
                if (entity.TryGet<PositionComponent>(out var position) &&
                    entity.TryGet<VelocityComponent>(out var velocity))
                {
                    // Update position based on velocity and delta time
                    entity.AddOrReplaceComponent(new PositionComponent
                    {
                        Value = position.Value + velocity.Value * (float)SharedConstants.FixedDeltaTime.TotalSeconds
                    });
                }
            }
        }
    }
}