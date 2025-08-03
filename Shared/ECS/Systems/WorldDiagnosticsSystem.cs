using System;
using System.Linq;
using Shared.ECS.Simulation;

namespace Shared.ECS.Systems
{
    /// <summary>
    /// System that logs the number of entities in the world on each tick.
    /// </summary>
    [TickInterval(50)]
    public class WorldDiagnosticsSystem : ISystem
    {
        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            Console.WriteLine($"Tick {tickNumber} Delta: {deltaTime} - Entities: {entityRegistry.GetAll().Count()}");
        }
    }
}