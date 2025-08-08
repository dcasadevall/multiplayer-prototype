using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Shared.Scheduling;
using UnityEngine;

namespace Core.Scheduling
{
    /// <summary>
    /// Unity implementation of <see cref="IScheduler"/> that ensures tasks run on the main thread using coroutines.
    /// This scheduler guarantees execution within Unity's update loop, which is required for any interaction
    /// with Unity's UI and other game components.
    /// that if a scheduled task takes longer than the period to execute, the next invocation will be delayed.
    /// It will wait for the full period after the long task completes, effectively "dropping" ticks to
    /// prevent a "spiral of death" and prioritize application responsiveness over strict adherence to the schedule.
    /// This is in contrast to the server's <see cref="Shared.Scheduling.TimerScheduler"/>, which will attempt
    /// to catch up by running missed ticks in rapid succession.
    /// </summary>
    public class UnityMainThreadScheduler : MonoBehaviour, IScheduler
    {
        private class CoroutineDisposable : IDisposable
        {
            private readonly MonoBehaviour _runner;
            private readonly Coroutine _coroutine;
            private bool _disposed;

            public CoroutineDisposable(MonoBehaviour runner, Coroutine coroutine)
            {
                _runner = runner;
                _coroutine = coroutine;
            }

            public void Dispose()
            {
                if (_disposed) return;
                
                if (_runner != null && _coroutine != null)
                {
                    _runner.StopCoroutine(_coroutine);
                }
                _disposed = true;
            }
        }

        public IDisposable ScheduleAtFixedRate(Action action, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            var coroutine = StartCoroutine(RunActionPeriodically(action, initialDelay, period, cancellationToken));
            return new CoroutineDisposable(this, coroutine);
        }

        private static IEnumerator RunActionPeriodically(Action action, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken)
        {
            if (initialDelay > TimeSpan.Zero)
            {
                yield return new WaitForSeconds((float)initialDelay.TotalSeconds);
            }

            var waitForPeriod = new WaitForSeconds((float)period.TotalSeconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UnityMainThreadScheduler] Error in scheduled action: {e}");
                }

                yield return waitForPeriod;
            }
        }
    }
}