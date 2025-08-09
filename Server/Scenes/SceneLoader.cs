using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.ECS.Replication;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.Physics;

namespace Server.Scenes
{
    public class EntityDescription
    {
        [JsonPropertyName("archetype")]
        public string Archetype { get; set; } = String.Empty;

        [JsonPropertyName("components")]
        public Dictionary<string, JsonElement> Components { get; set; } = new();
    }

    public class SceneLoader(EntityRegistry entityRegistry)
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

            foreach (var desc in entityDescriptions)
            {
                if (desc.Archetype == "Bot")
                {
                    var position = JsonSerializer.Deserialize<PositionComponent>(desc.Components["PositionComponent"].GetRawText());
                    BotArchetype.Create(entityRegistry, position?.Value ?? Vector3.Zero);
                }
            }
        }
    }
}