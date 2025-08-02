using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shared.ECS.Replication
{
    public class WorldSnapshotMessage
    {
        [JsonPropertyName("entities")] public List<SnapshotEntity> Entities { get; set; } = new();
    }

    public class SnapshotEntity
    {
        [JsonPropertyName("id")] public Guid Id { get; set; }

        [JsonPropertyName("components")] public List<SnapshotComponent> Components { get; set; } = new();
    }

    public class SnapshotComponent
    {
        [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

        [JsonPropertyName("json")] public string Json { get; set; } = string.Empty;
    }
}