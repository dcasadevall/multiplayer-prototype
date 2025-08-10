using Shared.ECS;
using Shared.Replication;

namespace Server.AI
{
    /// <summary>
    /// Component used to track the cooldown for shooting actions.
    /// </summary>
    public class ShootingCooldownComponent : INonReplicatedComponent
    {
        /// <summary>
        /// When the cooldown ends, represented as a tick count.
        /// </summary>
        public uint EndTick { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(EndTick);
        }

        public void Deserialize(IComponentReader reader)
        {
            EndTick = reader.GetUInt();
        }
    }
}