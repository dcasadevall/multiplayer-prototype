using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Shared.Scheduling
{
    /// <summary>
    /// A <see cref="SynchronizationContext"/> that executes all posted work on a single, dedicated thread.
    /// This is useful in server or console environments where a default synchronization context is not available,
    /// but single-threaded execution is required for a series of tasks.
    /// For example, for running the world simulation on a single thread.
    /// </summary>
    public sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly Thread _thread;
        private bool _disposed;

        public SingleThreadSynchronizationContext()
        {
            _thread = new Thread(RunOnThread)
            {
                IsBackground = true, // Ensure thread doesn't prevent application from exiting
                Name = "SingleThreadScheduler"
            };
            _thread.Start();
        }

        private void RunOnThread()
        {
            SetSynchronizationContext(this);

            foreach (var action in _queue.GetConsumingEnumerable())
            {
                action();
            }
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (_disposed) return;
            _queue.Add(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("Synchronous 'Send' is not supported by this context.");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _queue.CompleteAdding();
            _thread.Join();
            _queue.Dispose();
        }
    }
}
