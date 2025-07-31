namespace Shared.Networking.Replication;

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
    public required string Type { get; set; }
    public required string Json { get; set; }
}