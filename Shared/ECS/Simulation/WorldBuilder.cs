using Shared.Clock;
using Shared.Scheduling;

namespace Shared.ECS.Simulation;

/// <summary>
/// Builder class for creating ECS worlds with deterministic fixed timestep simulation.
/// </summary>
public class WorldBuilder
{
    private readonly List<ISystem> _systems = [];
    private readonly IClock _clock;
    private readonly EntityRegistry _entityRegistry;
    private readonly IScheduler _scheduler = new TimerScheduler();
    private TimeSpan _tickRate = TimeSpan.FromMilliseconds(33.33); // 30Hz default

    /// <summary>
    /// Initializes a new <see cref="WorldBuilder"/> with the given clock and entity registry.
    /// </summary>
    /// <param name="clock">The clock to use for timing.</param>
    /// <param name="entityRegistry">The entity registry to use.</param>
    public WorldBuilder(IClock clock, EntityRegistry entityRegistry)
    {
        _clock = clock;
        _entityRegistry = entityRegistry;
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
    /// Builds a world with fixed timestep simulation.
    /// All systems run at a constant rate with deterministic behavior.
    /// </summary>
    /// <returns>A new <see cref="World"/> instance.</returns>
    public World Build()
    {
        return new World(_systems, _clock, _entityRegistry, _tickRate, _scheduler);
    }

    /// <summary>
    /// Builds a world with the specified tick rate.
    /// </summary>
    /// <param name="tickRate">The time between ticks.</param>
    /// <returns>A new <see cref="World"/> instance.</returns>
    public World Build(TimeSpan tickRate)
    {
        return new World(_systems, _clock, _entityRegistry, tickRate, _scheduler);
    }

    /// <summary>
    /// Builds a world with the specified frequency.
    /// </summary>
    /// <param name="frequencyHz">The frequency in Hz.</param>
    /// <returns>A new <see cref="World"/> instance.</returns>
    public World Build(int frequencyHz)
    {
        var tickRate = TimeSpan.FromMilliseconds(1000.0 / frequencyHz);
        return new World(_systems, _clock, _entityRegistry, tickRate, _scheduler);
    }
} 