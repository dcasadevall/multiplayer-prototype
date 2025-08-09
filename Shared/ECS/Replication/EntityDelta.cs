using System;
using System.Collections.Generic;

namespace Shared.ECS.Replication
{
    public class EntityDelta
    {
        public Guid EntityId { get; set; }
        public bool IsNew { get; set; }
        public List<IComponent> AddedOrModifiedComponents { get; set; } = new();
        public List<Type> RemovedComponents { get; set; } = new();
    }
}
