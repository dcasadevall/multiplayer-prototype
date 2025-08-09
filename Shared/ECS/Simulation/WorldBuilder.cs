using System;
using System.Collections.Generic;
using Shared.ECS.Entities;
using Shared.ECS.TickSync;
using Shared.Scheduling;

namespace Shared.ECS.Simulation
{
    /// <summary>
    /// Builder class for creating ECS worlds with deterministic fixed timestep simulation.
    /// </summary>
    public class WorldBuilder
    {
        private readonly List<ISystem> _systems = new List<ISystem>();
        private readonly EntityRegistry _entityRegistry;
        private readonly ITickSync _tickSync;
        private readonly IScheduler _scheduler;
        private TimeSpan _tickRate = TimeSpan.FromMilliseconds(33.33); // 30Hz default
        private WorldMode _worldMode = WorldMode.Server;

        /// <summary>
        /// Initializes a new <see cref="WorldBuilder"/> with the given clock and entity registry.
        /// </summary>
        /// <param name="entityRegistry">The entity registry to use.</param>
        /// <param name="tickSync">Tick synchronization service for managing server and client ticks.</param>
        /// <param name="scheduler">The scheduler to use for managing ticks.</param>
        public WorldBuilder(EntityRegistry entityRegistry, ITickSync tickSync, IScheduler scheduler)
        {
            _entityRegistry = entityRegistry;
            _tickSync = tickSync;
            _scheduler = scheduler;
        }

        /// <summary>
        /// Adds a system to the world.
        /// </summary>
        /// <param name="system">The system to add.</param>
        /// <returns>This builder for method chaining.</returns>
        public WorldBuilder AddSystem(ISystem system)
        {
            _systems.Add(system);
            return this;
        }

        /// <summary>
        /// Sets the tick rate for the simulation.
        /// </summary>
        /// <param name="tickRate">The time between ticks.</param>
        /// <returns>This builder for method chaining.</returns>
        public WorldBuilder WithTickRate(TimeSpan tickRate)
        {
            _tickRate = tickRate;
            return this;
        }

        /// <summary>
        /// Sets the tick rate using frequency in Hz.
        /// </summary>
        /// <param name="frequencyHz">The frequency in Hz (e.g., 30 for 30Hz).</param>
        /// <returns>This builder for method chaining.</returns>
        public WorldBuilder WithFrequency(int frequencyHz)
        {
            _tickRate = TimeSpan.FromMilliseconds(1000.0 / frequencyHz);
            return this;
        }

        /// <summary>
        /// Sets the world mode (Server or Client).
        /// </summary>
        /// <param name="mode">The world mode.</param>
        /// <returns>This builder for method chaining.</returns>
        public WorldBuilder WithWorldMode(WorldMode mode)
        {
            _worldMode = mode;
            return this;
        }

        /// <summary>
        /// Builds a world with fixed timestep simulation.
        /// All systems run at a constant rate with deterministic behavior.
        /// </summary>
        /// <returns>A new <see cref="World"/> instance.</returns>
        public World Build()
        {
            return new World(_systems, _entityRegistry, _tickSync, _tickRate, _scheduler, _worldMode);
        }
    }
}