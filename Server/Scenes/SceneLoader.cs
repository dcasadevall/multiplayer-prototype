using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.ECS.Replication;

namespace Server.Scenes
{
    public class EntityDescription
    {
        [JsonPropertyName("components")] public Dictionary<string, JsonElement> Components { get; set; } = new();

        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    }

    public class SceneLoader(IWorldSnapshotConsumer snapshotConsumer)
    {
        /// <summary>
        /// Loads a scene from a JSON file and applies it to the registry using the snapshot consumer.
        /// </summary>
        /// <param name="path">Path to the scene JSON file.</param>
        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            var entityDescriptions = JsonSerializer.Deserialize<List<EntityDescription>>(json);

            if (entityDescriptions == null)
            {
                throw new InvalidOperationException($"Failed to deserialize scene from {path}");
            }

            // Convert EntityDescription list to WorldSnapshotMessage
            var snapshotMsg = new WorldSnapshotMessage
            {
                Entities = entityDescriptions.Select(desc =>
                {
                    var entityId = Guid.NewGuid();
                    var components = desc.Components.Select(kvp =>
                    {
                        var componentType = GetComponentTypeName(kvp.Key);
                        return new SnapshotComponent
                        {
                            Type = componentType,
                            Json = kvp.Value.GetRawText()
                        };
                    }).ToList();

                    return new SnapshotEntity
                    {
                        Id = entityId,
                        Components = components
                    };
                }).ToList()
            };

            // Serialize WorldSnapshotMessage to JSON and apply via consumer
            var snapshotJson = JsonSerializer.Serialize(snapshotMsg);
            snapshotConsumer.ConsumeSnapshot(System.Text.Encoding.UTF8.GetBytes(snapshotJson));
        }

        private static string GetComponentTypeName(string key)
        {
            // Map scene component keys to fully qualified type names as needed
            // For example, "PositionComponent" => "Shared.ECS.Components.PositionComponent, Shared"
            // Adjust this mapping as appropriate for your project
            return key switch
            {
                "PositionComponent" => "Shared.ECS.Components.PositionComponent, Shared",
                "HealthComponent" => "Shared.ECS.Components.HealthComponent, Shared",
                "ReplicatedTagComponent" => "Shared.ECS.Components.ReplicatedTagComponent, Shared",
                _ => key
            };
        }
    }
}