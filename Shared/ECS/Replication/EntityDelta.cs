using System;
using System.Collections.Generic;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Represents the changes (delta) to a single entity for replication between server and client.
    /// Used to efficiently synchronize only the differences in entity state, rather than full snapshots.
    /// </summary>
    public class EntityDelta
    {
        /// <summary>
        /// The unique identifier of the entity whose state has changed.
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Indicates whether this entity is newly created in this delta.
        /// If true, the client should create the entity; otherwise, apply changes to an existing entity.
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// If true, the entity has been destroyed and should be removed from the client-side registry.
        /// </summary>
        public bool IsDestroyed { get; set; }

        /// <summary>
        /// The list of components that have been added or modified on the entity since the last update.
        /// These should be applied to the entity on the receiving side.
        /// </summary>
        public List<IComponent> AddedOrModifiedComponents { get; set; } = new();

        /// <summary>
        /// The list of component types that have been removed from the entity since the last update.
        /// The receiver should remove these components from the entity.
        /// </summary>
        public List<Type> RemovedComponents { get; set; } = new();
    }
}