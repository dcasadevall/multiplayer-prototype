using System.Diagnostics;

namespace Shared.ECS;

/// <summary>
/// Manages the ECS world on the server, ticking all registered systems at a fixed interval.
/// </summary>
public class World : IDisposable
{
    private readonly List<ISystem> _systems = new();
    private CancellationTokenSource _cts;
    private bool _isRunning;
    private float _tickRate = 20f; // 20 ticks per second by default
    private Task _tickTask;
    public EntityManager EntityManager { get; } = new();

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }

    /// <summary>
    /// Add a system to the world. Call before Start().
    /// </summary>
    public void AddSystem(ISystem system)
    {
        _systems.Add(system);
    }

    /// <summary>
    /// Start ticking the world at the configured tick rate.
    /// </summary>
    public void Start(float? tickRate = null)
    {
        if (_isRunning)
            throw new InvalidOperationException("World is already running.");

        if (tickRate.HasValue)
            _tickRate = tickRate.Value;

        _cts = new CancellationTokenSource();
        _tickTask = Task.Run(() => TickLoop(_cts.Token));
        _isRunning = true;
    }

    /// <summary>
    /// Stop ticking the world.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;
        _cts.Cancel();
        try
        {
            _tickTask.Wait();
        }
        catch
        {
            /* ignore */
        }

        _isRunning = false;
    }

    private async Task TickLoop(CancellationToken token)
    {
        var tickInterval = TimeSpan.FromSeconds(1.0 / _tickRate);
        var sw = new Stopwatch();
        while (!token.IsCancellationRequested)
        {
            sw.Restart();
            var deltaTime = (float)tickInterval.TotalSeconds;
            foreach (var system in _systems) system.Update(EntityManager, deltaTime);
            var elapsed = sw.Elapsed;
            var delay = tickInterval - elapsed;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, token);
        }
    }
}