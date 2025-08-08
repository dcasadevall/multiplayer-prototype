using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Health
{
    /// <summary>
    /// Stores the health state of an entity.
    /// Used for all entities that can take damage or be destroyed.
    /// </summary>
    public class HealthComponent : IComponent
    {
        private int _maxHealth;
        private int _currentHealth;

        /// <summary>
        /// The maximum health value for the entity.
        /// Setting this also initializes current health.
        /// </summary>
        [JsonPropertyName("maxHealth")]
        public int MaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = value;
                _currentHealth = value; // Initialize current health to max health
            }
        }

        /// <summary>
        /// The current health value for the entity.
        /// </summary>
        [JsonPropertyName("currentHealth")]
        public int CurrentHealth
        {
            get => _currentHealth;
            set => _currentHealth = value;
        }

        /// <summary>
        /// Returns true if the entity is dead (health is zero or less).
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;

        public HealthComponent()
        {
        }

        /// <summary>
        /// Constructs a HealthComponent with the given max health.
        /// </summary>
        /// <param name="maxHealth">Maximum health value.</param>
        public HealthComponent(int maxHealth)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }
    }
}