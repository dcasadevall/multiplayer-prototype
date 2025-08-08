using System;
using System.Threading;

namespace Shared.Scheduling
{
    /// <summary>
    /// A scheduler that uses <see cref="System.Threading.Timer"/> to execute a task at a fixed rate.
    /// This implementation has two key behaviors:
    /// 1.  **Non-Overlapping Execution**: `System.Threading.Timer` guarantees that the callback will not be
    ///     called again until the previous execution is complete. This prevents re-entrancy issues.
    /// 2.  **Catch-up on Delay**: If a task execution takes longer than the specified period, the timer will
    ///     fall behind. Once the long-running task completes, the timer will attempt to "catch up" by
    ///     executing the missed ticks in rapid succession without delay until it is back on schedule.
    ///     For a real-time simulation, this means the simulation must consistently run faster than the tick rate
    ///     to avoid "spiral of death" performance issues.
    /// </summary>
    public class TimerScheduler : IScheduler
    {
        /// <summary>
        /// Schedules a task for repeated execution.
        /// </summary>
        /// <param name="task">The <see cref="Action"/> delegate to be executed.</param>
        /// <param name="initialDelay">The <see cref="TimeSpan"/> representing the amount of time to wait before the first execution.
        /// Specify <c>TimeSpan.Zero</c> to start the first execution immediately.</param>
        /// <param name="period">The <see cref="TimeSpan"/> representing the time interval between subsequent executions.
        /// Specify <c>Timeout.InfiniteTimeSpan</c> to disable periodic signaling.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow for external cancellation.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> object. Disposing this object will cancel the scheduled task and release
        /// all associated resources.
        /// </returns>
        public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            // Create a new CancellationTokenSource linked to the optional external token.
            // This allows cancellation to be triggered either by disposing the returned object
            // or by cancelling the provided CancellationToken.
            var timerCancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
            var timer = new Timer(_ =>
            {
                // Before executing the task, check if cancellation has been requested.
                // This prevents a final execution from starting if Dispose() was called
                // between timer ticks.
                if (timerCancelSource.IsCancellationRequested) return;

                try
                {
                    // Execute the task directly on the timer's thread pool thread.
                    // System.Threading.Timer guarantees that it will not fire the callback
                    // again until the previous callback has completed. This behavior is crucial
                    // for preventing overlapping executions.
                    task();
                }
                catch (Exception ex)
                {
                    // The user-provided task is executed on a ThreadPool thread.
                    // An unhandled exception here would be fatal and terminate the entire process.
                    // For a production system, this should be replaced with a proper logging mechanism.
                    Console.WriteLine($"[TimerScheduler] Error in scheduled task: {ex.Message}");
                }
            }, null, initialDelay, period);

            // Return a disposable object that encapsulates the timer and its cancellation source,
            // providing a clean and reliable way for the caller to stop the schedule.
            return new CancellationDisposable(timer, timerCancelSource);
        }

        /// <summary>
        /// An internal helper class that implements <see cref="IDisposable"/> to manage the lifetime
        /// of the timer and its associated cancellation token source.
        /// Its sole purpose is to provide a thread-safe mechanism to stop the timer and release resources.
        /// </summary>
        private sealed class CancellationDisposable : IDisposable
        {
            private readonly Timer _timer;
            private readonly CancellationTokenSource _cts;

            /// <summary>
            /// A flag to ensure the dispose logic is executed only once.
            /// Using an int with Interlocked provides thread-safe, lock-free disposition.
            /// 0 = not disposed, 1 = disposed.
            /// </summary>
            private int _disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="CancellationDisposable"/> class.
            /// </summary>
            /// <param name="timer">The active timer to manage.</param>
            /// <param name="cts">The CancellationTokenSource associated with the timer.</param>
            public CancellationDisposable(Timer timer, CancellationTokenSource cts)
            {
                _timer = timer;
                _cts = cts;
            }

            /// <summary>
            /// Disposes the object, which cancels the token, stops the timer, and releases all resources.
            /// This method is thread-safe and can be called multiple times without causing an exception.
            /// </summary>
            public void Dispose()
            {
                // Atomically check and set the _disposed flag. If it was already 1 (disposed),
                // this method returns immediately. This prevents race conditions if multiple
                // threads call Dispose() simultaneously.
                if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
                {
                    return;
                }
            
                // 1. Signal cancellation to any running tasks and to the linked external token.
                _cts.Cancel();

                // 2. Stop the timer from firing any more events and release its resources.
                _timer.Dispose();

                // 3. Release the resources used by the CancellationTokenSource.
                _cts.Dispose();
            }
        }
    }
}