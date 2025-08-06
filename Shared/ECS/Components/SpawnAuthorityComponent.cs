using System;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("spawnedByPeerId")]
        public int SpawnedByPeerId { get; set; }

        /// <summary>
        /// The local entity ID that was used when this entity was first predicted/spawned on the client.
        /// Used to associate server entities with client-predicted entities.
        /// </summary>
        [JsonPropertyName("localEntityId")]
        public Guid LocalEntityId { get; set; }

        /// <summary>
        /// The tick at which this entity was spawned.
        /// </summary>
        [JsonPropertyName("spawnTick")]
        public uint SpawnTick { get; set; }
    }
}