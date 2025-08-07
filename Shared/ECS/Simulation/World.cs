using System;
using System.Collections.Generic;
using System.Threading;
using Shared.ECS.TickSync;
using Shared.Scheduling;

namespace Shared.ECS.Simulation
{
    /// <summary>
    /// Represents an ECS world that manages entity and system lifecycles with a fixed timestep simulation.
    /// 
    /// <para>
    /// The <c>World</c> class provides:
    /// <list type="bullet">
    ///   <item>Deterministic simulation with fixed timesteps</item>
    ///   <item>Discrete tick indexes for unambiguous event scheduling</item>
    ///   <item>System scheduling based on tick intervals rather than real time</item>
    ///   <item>Lifecycle control: <see cref="Start"/>, <see cref="Stop"/>, and <see cref="Dispose"/></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// On <see cref="Start"/>, the world launches a background task that runs at a constant rate.
    /// Each iteration is a "tick" with a fixed delta time. Systems are updated based on their
    /// <see cref="TickIntervalAttribute"/>, ensuring deterministic behavior regardless of
    /// real-world performance fluctuations.
    /// </para>
    /// </summary>
    public class World : IDisposable
    {
        private readonly List<SystemSchedule> _scheduledSystems = new List<SystemSchedule>();
        private readonly IScheduler _scheduler;
        private readonly EntityRegistry _entityRegistry;
        private readonly ITickSync _tickSync;

        private CancellationTokenSource? _cancelTokenSource;
        private IDisposable? _tickDisposable;
        private bool _isRunning;
        private uint _tickNumber;
        private readonly float _fixedDeltaTime;
        private readonly TimeSpan _tickRate;

        /// <summary>
        /// Gets the current tick index. This represents the number of simulation steps
        /// that have been processed since the world started.
        /// </summary>
        public uint CurrentTickIndex => _tickNumber;

        private bool isClient = false;

        /// <summary>
        /// Initializes a new <see cref="World"/> with the given systems and configuration.
        /// </summary>
        /// <param name="startingTick">The initial tick number to start from.</param>
        /// <param name="systems">The systems to register with this world.</param>
        /// <param name="entityRegistry">Registry used for managing entities in this world.</param>
        /// <param name="tickRate">The time between ticks (e.g., 33ms for 30Hz).</param>
        /// <param name="scheduler">The scheduler to use for driving ticks.</param>
        internal World(uint startingTick,
            IEnumerable<ISystem> systems,
            EntityRegistry entityRegistry,
            ITickSync tickSync,
            TimeSpan tickRate,
            IScheduler scheduler)
        {
            _entityRegistry = entityRegistry;
            _tickSync = tickSync;
            _tickRate = tickRate;
            _fixedDeltaTime = (float)tickRate.TotalSeconds;
            _tickNumber = startingTick;
            _scheduler = scheduler;

            // Create scheduled systems
            foreach (var system in systems)
            {
                _scheduledSystems.Add(new SystemSchedule(system));
            }

            isClient = startingTick > 0;
        }

        /// <summary>
        /// Starts the fixed timestep simulation loop.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("World is already running.");
            }

            _cancelTokenSource = new CancellationTokenSource();
            _tickDisposable = _scheduler.ScheduleAtFixedRate(
                Tick,
                TimeSpan.Zero,
                _tickRate,
                _cancelTokenSource.Token
            );
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
            _tickDisposable?.Dispose();

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
        /// The main fixed timestep simulation loop.
        ///
        /// <para>
        /// Runs at a constant rate, processing one simulation step per iteration.
        /// Each step increments the tick index and updates systems that are due to run
        /// based on their tick intervals. This ensures deterministic behavior.
        /// </para>
        /// </summary>
        private void Tick()
        {
            // Update systems that should run on this tick
            foreach (var scheduledSystem in _scheduledSystems)
            {
                if (scheduledSystem.ShouldRun(_tickNumber))
                {
                    scheduledSystem.System.Update(_entityRegistry, _tickNumber, _fixedDeltaTime);
                }
            }

            if (!isClient)
            {
                _tickNumber++;
            }
            else
            {
                _tickNumber = _tickSync.ClientTick;
            }
        }
    }
}