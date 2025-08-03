using System;
using System.Collections.Generic;
using System.Threading;

namespace Shared.Scheduling
{
    /// <summary>
    /// Ticks all registered ITickable instances using the injected IScheduler.
    /// </summary>
    public class TickableScheduler : IInitializable, IDisposable
    {
        private readonly IEnumerable<ITickable> _tickables;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TickableScheduler(IEnumerable<ITickable> tickables, IScheduler scheduler)
        {
            _tickables = tickables;
            _scheduler = scheduler;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            // Schedule the Tick method to be called every frame
            _scheduler.ScheduleAtFixedRate(Tick, 
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1.0f / 60.0f), // 60 FPS
                _cancellationTokenSource.Token);
        }

        private void Tick()
        {
            foreach (var tickable in _tickables)
            {
                tickable.Tick();
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
