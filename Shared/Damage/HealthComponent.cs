using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Damage
{
    /// <summary>
    /// Stores the health state of an entity.
    /// Used for all entities that can take damage or be destroyed.
    /// </summary>
    public class HealthComponent : IComponent
    {
        /// <summary>
        /// The maximum health value for the entity.
        /// Setting this also initializes current health.
        /// </summary>
        [JsonPropertyName("maxHealth")]
        public int MaxHealth { get; set; }

        /// <summary>
        /// The current health value for the entity.
        /// </summary>
        [JsonPropertyName("currentHealth")]
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Returns true if the entity is dead (health is zero or less).
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;
    }
}