using System.Collections.Generic;

namespace Shared.ECS.Replication
{
    public class WorldDeltaMessage
    {
        public List<EntityDelta> Deltas { get; set; } = new();
    }
}