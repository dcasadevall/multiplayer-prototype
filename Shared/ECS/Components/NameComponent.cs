using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Component that assigns a name to an entity.
    /// </summary>
    public class NameComponent : IComponent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}