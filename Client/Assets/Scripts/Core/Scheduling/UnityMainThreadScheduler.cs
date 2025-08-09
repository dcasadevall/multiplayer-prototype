using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Shared.Scheduling;
using UnityEngine;
using System.Collections.Concurrent;

namespace Core.Scheduling
{
    /// <summary>
    /// Unity implementation of <see cref="IScheduler"/> that ensures tasks run on the main thread using coroutines.
    /// This scheduler leverages a <see cref="MonoBehaviour"/> to execute tasks on Unity's main thread,
    /// which is essential for any operations that interact with the Unity API.
    /// </summary>
    public class UnityMainThreadScheduler : IScheduler
    {
        private readonly MainThreadDispatcher _dispatcher;

        /// <summary>
        /// A nested <see cref="MonoBehaviour"/> that serves as the execution context for scheduled tasks.
        /// It is responsible for invoking the actions on the main thread during Unity's `Update` loop.
        /// </summary>
        private class MainThreadDispatcher : MonoBehaviour
        {
            private readonly ConcurrentQueue<Action> _executionQueue = new ConcurrentQueue<Action>();

            public void Update()
            {
                while (_executionQueue.TryDequeue(out var action))
                {
                    action.Invoke();
                }
            }

            public void Enqueue(Action action)
            {
                _executionQueue.Enqueue(action);
            }
        }

        public UnityMainThreadScheduler()
        {
            var go = new GameObject("MainThreadScheduler");
            _dispatcher = go.AddComponent<MainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public IDisposable ScheduleAtFixedRate(Action action, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            var timer = new Timer(
                _ => _dispatcher.Enqueue(action),
                null,
                initialDelay,
                period);

            cancellationToken.Register(() => timer.Dispose());

            return timer;
        }

        public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, SynchronizationContext context, CancellationToken cancellationToken = default)
        {
            // Since this scheduler *always* runs on the main thread, we can just call the other overload.
            return ScheduleAtFixedRate(task, initialDelay, period, cancellationToken);
        }
    }
}