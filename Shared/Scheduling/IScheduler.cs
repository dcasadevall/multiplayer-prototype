namespace Shared.Scheduling;

/// <summary>
/// Interface for scheduling tasks.
/// Provides a common abstraction for different scheduling implementations.
/// </summary>
public interface IScheduler
{
    /// <summary>
    /// Schedules a task to be executed periodically at a fixed rate.
    /// </summary>
    /// <param name="task">The task to be executed.</param>
    /// <param name="initialDelay">The delay before the first execution.</param>
    /// <param name="period">The time between successive executions.</param>
    /// <param name="cancellationToken">Token to cancel the scheduled task.</param>
    /// <returns>An IDisposable that can be used to cancel the scheduled task.</returns>
    IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default);
}