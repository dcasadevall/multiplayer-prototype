using System.Linq;
using Shared.ECS.Entities;
using Shared.Logging;

namespace Shared.ECS.Simulation
{
    /// <summary>
    /// System that logs the number of entities in the world on each tick.
    /// </summary>
    [TickInterval(50)]
    public class WorldDiagnosticsSystem : ISystem
    {
        private readonly ILogger _logger;

        public WorldDiagnosticsSystem(ILogger logger)
        {
            _logger = logger;
        }

        public void Update(EntityRegistry entityRegistry, uint tickNumber, float deltaTime)
        {
            _logger.Debug(LoggedFeature.Simulation, $"Tick {tickNumber} Delta: {deltaTime} - Entities: {entityRegistry.GetAll().Count()}");
        }
    }
}