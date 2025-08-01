using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Shared.Scheduling;
using UnityEngine;

namespace Core.Scheduling
{
    /// <summary>
    /// Unity implementation of IScheduler that ensures tasks run on the main thread.
    /// Uses coroutines to schedule periodic tasks and ensures they execute in Unity's update loop.
    /// </summary>
    public class UnityMainThreadScheduler : MonoBehaviour, IScheduler
    {
        private class ScheduledTask : IDisposable
        {
            private readonly Action _task;
            private readonly float _periodSeconds;
            private readonly CancellationToken _cancellationToken;
            private readonly MonoBehaviour _coroutineRunner;
            private Coroutine _coroutine;

            public ScheduledTask(Action task, float periodSeconds, CancellationToken cancellationToken, MonoBehaviour coroutineRunner)
            {
                _task = task;
                _periodSeconds = periodSeconds;
                _cancellationToken = cancellationToken;
                _coroutineRunner = coroutineRunner;
                
                // Start the coroutine immediately
                _coroutine = _coroutineRunner.StartCoroutine(RunTaskPeriodically());
            }

            private IEnumerator RunTaskPeriodically()
            {
                while (!_cancellationToken.IsCancellationRequested && _coroutine != null)
                {
                    try
                    {
                        _task.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error in scheduled task: {e}");
                    }

                    yield return new WaitForSeconds(_periodSeconds);
                }
            }

            public void Dispose()
            {
                _coroutineRunner?.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private readonly List<ScheduledTask> _activeTasks = new();

        /// <summary>
        /// Schedules a task to be executed periodically at a fixed rate on Unity's main thread.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="initialDelay">The delay before the first execution.</param>
        /// <param name="period">The time between successive executions.</param>
        /// <param name="cancellationToken">Token to cancel the scheduled task.</param>
        /// <returns>An IDisposable that can be used to cancel the scheduled task.</returns>
        public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            // Create a new task that first waits for the initial delay
            var wrappedTask = new Action(() =>
            {
                if (initialDelay > TimeSpan.Zero)
                {
                    // For the initial delay, we'll use a coroutine
                    StartCoroutine(DelayedStart(task, initialDelay));
                }
                else
                {
                    task();
                }
            });

            // Create and store the scheduled task
            var scheduledTask = new ScheduledTask(
                wrappedTask, 
                (float)period.TotalSeconds,
                cancellationToken,
                this);
            
            _activeTasks.Add(scheduledTask);
            return scheduledTask;
        }

        private IEnumerator DelayedStart(Action task, TimeSpan delay)
        {
            yield return new WaitForSeconds((float)delay.TotalSeconds);
            task();
        }

        private void OnDestroy()
        {
            _activeTasks.ForEach(x => x.Dispose());
            _activeTasks.Clear();
        }
    }
}