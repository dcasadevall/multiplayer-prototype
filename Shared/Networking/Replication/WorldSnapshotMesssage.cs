using System;
using System.Collections.Generic;

namespace Shared.Networking.Replication
{
    public class WorldSnapshotMessage
    {
        public List<SnapshotEntity> Entities { get; set; } = new(); 
    }

    public class SnapshotEntity
    {
        public Guid Id { get; set; }
        public List<SnapshotComponent> Components { get; set; } = new();
    }

    public class SnapshotComponent
    {
        public string Type { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
    }
}