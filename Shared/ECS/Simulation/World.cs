using System.Reflection;

namespace Shared.ECS.Simulation;

/// <summary>
/// Represents an ECS world that manages entity and system lifecycles, and drives simulation ticks.
/// 
/// <para>
/// The <c>World</c> class is responsible for:
/// <list type="bullet">
///   <item>Registering and managing all systems for the simulation.</item>
///   <item>Maintaining an <see cref="EntityRegistry"/> for entity/component storage.</item>
///   <item>Driving the simulation loop, ticking each system at its configured interval (using <see cref="TickRateMsAttribute"/> or a default).</item>
///   <item>Providing lifecycle control: <see cref="Start"/>, <see cref="Stop"/>, and <see cref="Dispose"/>.</item>
/// </list>
/// </para>
/// 
/// <para>
/// On <see cref="Start"/>, the world launches a background task that repeatedly checks each system's last tick time.
/// If the elapsed time since the last tick exceeds the system's interval, the system's <c>Update</c> method is called,
/// and the tick time is updated. This allows each system to run at its own rate, decoupled from other systems.
/// </para>
/// </summary>
public class World : IDisposable
{
    private readonly List<ISystem> _systems = [];
    private readonly IClock _clock;
    private readonly EntityRegistry _entityRegistry;

    private CancellationTokenSource? _cancelTokenSource;
    private bool _isRunning;
    private int _defaultTickRateMs = 50;
    private Task? _tickTask;

    /// <summary>
    /// Initializes a new <see cref="World"/> with the given systems and clock.
    /// </summary>
    /// <param name="systems">The systems to register with this world.</param>
    /// <param name="clock">The clock to use for ticks.</param>
    /// <param name="entityRegistry">Registry used for managing entities in this world</param>
    internal World(IEnumerable<ISystem> systems, IClock clock, EntityRegistry entityRegistry)
    {
        _clock = clock;
        _entityRegistry = entityRegistry;
        _systems.AddRange(systems);
    }

    /// <summary>
    /// Starts the simulation loop for this world.
    /// 
    /// <para>
    /// Launches a background task that ticks all registered systems at their configured intervals.
    /// Each system's tick interval is determined by its <see cref="TickRateMsAttribute"/>, or the provided <paramref name="defaultTickRateMs"/> if not specified.
    /// </para>
    /// </summary>
    /// <param name="defaultTickRateMs">Default tick interval (ms) for systems without a <see cref="TickRateMsAttribute"/>.</param>
    public void Start(int defaultTickRateMs = 50)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("World is already running.");
        }

        _cancelTokenSource = new CancellationTokenSource();
        _defaultTickRateMs = defaultTickRateMs;
        _tickTask = Task.Run(() => TickLoop(_cancelTokenSource.Token));
        _isRunning = true;
    }

    /// <summary>
    /// Stops the simulation loop and waits for the background task to complete.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _cancelTokenSource?.Cancel();
        _tickTask?.Wait();

        _isRunning = false;
    }

    /// <summary>
    /// Disposes the world, stopping the simulation loop and releasing resources.
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    /// The main simulation loop.
    /// 
    /// <para>
    /// For each registered system, tracks its last tick time and interval.
    /// On each iteration, checks if enough time has elapsed since the last tick for each system.
    /// If so, calls <c>Update</c> on the system, passing the elapsed time in seconds, and updates the last tick time.
    /// This allows each system to run at its own tick rate, independent of other systems.
    /// </para>
    /// </summary>
    /// <param name="token">Cancellation token to stop the loop.</param>
    private async Task TickLoop(CancellationToken token)
    {
        var schedules = _systems.Select(system =>
        {
            var attr = system.GetType().GetCustomAttribute<TickRateMsAttribute>();
            var interval = attr?.IntervalMs ?? _defaultTickRateMs;
            return new SystemSchedule(system, interval);
        }).ToList();

        while (!token.IsCancellationRequested)
        {
            var now = _clock.UtcNow;

            foreach (var s in schedules)
            {
                var delta = (now - s.LastTick).TotalMilliseconds;
                if (delta >= s.IntervalMs)
                {
                    s.System.Update(_entityRegistry, (float)(delta / 1000.0));
                    s.LastTick = now;
                }
            }

            await Task.Delay(10, token);
        }
    }
}