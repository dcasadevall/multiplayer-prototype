using System;
using System.Threading;

namespace Shared.Scheduling
{
    /// <summary>
    /// Interface for scheduling tasks.
    /// Provides a common abstraction for different scheduling implementations.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Schedules a task for repeated execution on the thread pool.
        /// </summary>
        /// <param name="task">The <see cref="Action"/> delegate to be executed.</param>
        /// <param name="initialDelay">The <see cref="TimeSpan"/> representing the amount of time to wait before the first execution.</param>
        /// <param name="period">The <see cref="TimeSpan"/> representing the time interval between subsequent executions.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow for external cancellation.</param>
        /// <returns>An <see cref="IDisposable"/> object that can be used to cancel the scheduled task.</returns>
        IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedules a task for repeated execution on a specific <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="task">The <see cref="Action"/> delegate to be executed.</param>
        /// <param name="initialDelay">The <see cref="TimeSpan"/> representing the amount of time to wait before the first execution.</param>
        /// <param name="period">The <see cref="TimeSpan"/> representing the time interval between subsequent executions.</param>
        /// <param name="context">The <see cref="SynchronizationContext"/> to post the task execution to.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> to allow for external cancellation.</param>
        /// <returns>An <see cref="IDisposable"/> object that can be used to cancel the scheduled task.</returns>
        IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, SynchronizationContext context, CancellationToken cancellationToken = default);
    }
}