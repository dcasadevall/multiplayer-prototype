using System;

namespace Shared.ECS.Simulation
{
    /// <summary>
    /// Attribute to specify the desired tick interval for a system.
    /// When applied to a system class, this attribute informs the world's ticker how often
    /// the system should be updated. If not present, the system runs every tick (interval of 1).
    /// 
    /// Usage:
    /// <code>
    /// [TickInterval(2)] // System will be updated every 2nd tick
    /// public class MySystem : ISystem { ... }
    /// 
    /// [TickInterval(10)] // System will be updated every 10th tick
    /// public class SlowSystem : ISystem { ... }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TickIntervalAttribute : Attribute
    {
        /// <summary>
        /// The tick interval for the decorated system.
        /// The system will run when (worldTickIndex % Interval == 0).
        /// A value of 1 means the system runs every tick.
        /// </summary>
        public uint Interval { get; }

        public TickIntervalAttribute(uint interval)
        {
            Interval = interval;
        }
    }
} 