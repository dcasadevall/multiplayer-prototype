using System;
using Shared.ECS;
using Shared.Replication;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Component that indicates which peer spawned this entity and tracks spawn authority.
    /// Used for client-side prediction to prevent duplicate spawning of replicated entities.
    /// </summary>
    public class SpawnAuthorityComponent : IComponent
    {
        /// <summary>
        /// The peer ID of the client that spawned this entity.
        /// </summary>
        public int SpawnedByPeerId { get; set; }

        /// <summary>
        /// The local entity ID that was used when this entity was first predicted/spawned on the client.
        /// Used to associate server entities with client-predicted entities.
        /// </summary>
        public Guid LocalEntityId { get; set; }

        /// <summary>
        /// The tick at which this entity was spawned.
        /// </summary>
        public uint SpawnTick { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(SpawnedByPeerId);
            writer.Put(LocalEntityId);
            writer.Put(SpawnTick);
        }

        public void Deserialize(IComponentReader reader)
        {
            SpawnedByPeerId = reader.GetInt();
            LocalEntityId = reader.GetGuid();
            SpawnTick = reader.GetUInt();
        }
    }
}