using System;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Replication policy for a component type.
    /// </summary>
    public enum ReplicationMode
    {
        /// <summary>
        /// Component is replicated on entity creation and on subsequent modifications.
        /// </summary>
        Always = 0,
        /// <summary>
        /// Component is replicated only on entity creation; modifications are not replicated.
        /// </summary>
        InitialOnly = 1,
        /// <summary>
        /// Component is never replicated.
        /// </summary>
        Never = 2,
        /// <summary>
        /// Component represents derived state; replicate initial data only (treated as InitialOnly).
        /// </summary>
        Derived = 3
    }

    /// <summary>
    /// Declares the replication policy for a component type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ReplicationAttribute : Attribute
    {
        public ReplicationMode Mode { get; }
        public ReplicationAttribute(ReplicationMode mode)
        {
            Mode = mode;
        }
    }
}
