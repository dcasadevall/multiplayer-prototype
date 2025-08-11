using System.Linq;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Settings;

namespace Shared.Physics
{
    /// <summary>
    /// System that handles entity movement based on velocity.
    /// Runs every tick to ensure smooth movement.
    /// </summary>
    [TickInterval(1)] // Run every tick
    public class VelocitySystem : ISystem
    {
        private readonly SimulationSettings _simulationSettings;

        public VelocitySystem(SimulationSettings simulationSettings)
        {
            _simulationSettings = simulationSettings;
        }

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
                    // Do not replace unless there is a change (this avoids unnecessary replication)
                    // Ideally, we shouldn't have to compare here, but our system is not performing
                    // equality checks on diffing.
                    var newPosition = position.Value + velocity.Value * (float)_simulationSettings.FixedDeltaTime.TotalSeconds;
                    if (newPosition != position.Value)
                    {
                        entity.AddOrReplaceComponent(new PositionComponent
                        {
                            Value = position.Value + velocity.Value * (float)_simulationSettings.FixedDeltaTime.TotalSeconds
                        });
                    }
                }
            }
        }
    }
}