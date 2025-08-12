using Shared.ECS;
using Shared.Replication;

namespace Shared.Damage
{
    /// <summary>
    /// Marks an entity as invulnerable until the specified server tick.
    /// Systems should respect this and avoid dealing damage/targeting.
    /// </summary>
    public class InvulnerableComponent : IComponent
    {
        public uint EndsAtTick { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(EndsAtTick);
        }

        public void Deserialize(IComponentReader reader)
        {
            EndsAtTick = reader.GetUInt();
        }
    }
}