using System.Reflection;

namespace Shared.ECS.Simulation
{
    /// <summary>
    /// Represents the scheduling metadata for a single system within a fixed-timestep world.
    /// Each <see cref="SystemSchedule"/> tracks the system instance and its desired tick interval.
    /// The world's ticker uses this information to determine when each system should be updated
    /// based on the current tick index, ensuring deterministic simulation.
    /// 
    /// Usage:
    /// - Each system is wrapped in a <see cref="SystemSchedule"/> with its tick interval.
    /// - On each world tick, the ticker checks if the current tick index is divisible by the interval.
    /// - If so, the system's <c>Update</c> method is called with a fixed delta time.
    /// This enables deterministic, per-system ticking tied to simulation steps rather than real time.
    /// </summary>
    internal class SystemSchedule
    {
        /// <summary>
        /// The system instance to be scheduled.
        /// </summary>
        public ISystem System { get; }

        /// <summary>
        /// The tick interval for this system.
        /// The system will only be updated when (tickIndex % Interval == 0).
        /// A value of 1 means the system runs every tick.
        /// </summary>
        public uint Interval { get; }

        /// <summary>
        /// Initializes a new <see cref="SystemSchedule"/> for the given system.
        /// </summary>
        /// <param name="system">The system to schedule.</param>
        public SystemSchedule(ISystem system)
        {
            System = system;
        
            // Get the tick interval from the attribute, defaulting to 1 (every tick)
            var attr = system.GetType().GetCustomAttribute<TickIntervalAttribute>();
            Interval = attr?.Interval ?? 1;
        }

        /// <summary>
        /// Determines if this system should run on the given tick index.
        /// </summary>
        /// <param name="tickIndex">The current world tick index.</param>
        /// <returns>True if the system should run on this tick, false otherwise.</returns>
        public bool ShouldRun(uint tickIndex)
        {
            return tickIndex % Interval == 0;
        }
    }
} 