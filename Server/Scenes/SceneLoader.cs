using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.ECS.Archetypes;
using Shared.ECS.Entities;

namespace Server.Scenes
{
    public class EntityDescription
    {
        [JsonPropertyName("archetype")]
        public string Archetype { get; set; } = String.Empty;

        [JsonPropertyName("components")]
        public Dictionary<string, JsonElement> Components { get; set; } = new();
    }

    public class SceneLoader(BotFactory botFactory)
    {
        private class JsonPosition
        {
            public float X { get; init; }
            public float Y { get; init; }
            public float Z { get; init; }
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
                    var jsonPosition =
                        JsonSerializer.Deserialize<JsonPosition>(desc.Components["PositionComponent"].GetRawText(), JsonOptions);
                    var position = jsonPosition != null ? new Vector3(jsonPosition.X, jsonPosition.Y, jsonPosition.Z) : Vector3.Zero;
                    botFactory.Create(position);
                }
            }
        }
    }
}