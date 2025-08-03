using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Indicates that the entity is associated with a prefab.
    /// <para>
    /// The <see cref="PrefabComponent"/> is used to tag entities that should be instantiated
    /// from a prefab resource. The <see cref="PrefabName"/> property specifies the name of the
    /// prefab to use. In development, this typically refers to a prefab in the Resources folder.
    /// For production, a prefab manifest or asset bundles should be used for more robust asset management.
    /// </para>
    /// </summary>
    public class PrefabComponent : IComponent
    {
        /// <summary>
        /// The name of the prefab resource associated with this entity.
        /// </summary>
        [JsonPropertyName("prefabName")]
        public string PrefabName { get; set; } = string.Empty;
    }
}