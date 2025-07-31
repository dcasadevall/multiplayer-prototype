namespace Shared.Networking;

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
    public string Type { get; set; } = null!;
    public string Json { get; set; } = null!;
}