using System;
using Shared.ECS;
using Shared.Replication;

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
        public int Damage { get; set; }

        /// <summary>
        /// The ID of the entity that spawned this damaging entity.
        /// </summary>
        public Guid SourceEntityId { get; set; }

        /// <summary>
        /// Whether this damage can affect the entity that spawned it (friendly fire).
        /// </summary>
        public bool CanDamageSelf { get; set; } = false;

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(Damage);
            writer.Put(SourceEntityId);
            writer.Put(CanDamageSelf);
        }

        public void Deserialize(IComponentReader reader)
        {
            Damage = reader.GetInt();
            SourceEntityId = reader.GetGuid();
            CanDamageSelf = reader.GetBool();
        }
    }
}