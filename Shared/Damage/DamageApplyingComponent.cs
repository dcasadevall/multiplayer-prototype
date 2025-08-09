using System;
using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Damage
{
    /// <summary>
    /// Component that defines how much damage an entity deals on impact.
    /// Used for projectiles, explosions, or any damaging entity.
    /// </summary>
    public class DamageApplyingComponent : IComponent
    {
        /// <summary>
        /// The amount of damage this entity deals when it hits a target.
        /// </summary>
        [JsonPropertyName("damage")]
        public int Damage { get; set; }

        /// <summary>
        /// The ID of the entity that spawned this damaging entity.
        /// </summary>
        [JsonPropertyName("sourceEntityId")]
        public Guid SourceEntityId { get; set; }

        /// <summary>
        /// Whether this damage can affect the entity that spawned it (friendly fire).
        /// </summary>
        [JsonPropertyName("canDamageSelf")]
        public bool CanDamageSelf { get; set; } = false;
    }
}