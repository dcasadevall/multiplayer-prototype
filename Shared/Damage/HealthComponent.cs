using Shared.ECS;
using Shared.ECS.Replication;

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
        public int MaxHealth { get; set; }

        /// <summary>
        /// The current health value for the entity.
        /// </summary>
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Returns true if the entity is dead (health is zero or less).
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(MaxHealth);
            writer.Put(CurrentHealth);
        }

        public void Deserialize(IComponentReader reader)
        {
            MaxHealth = reader.GetInt();
            CurrentHealth = reader.GetInt();
        }
    }
}