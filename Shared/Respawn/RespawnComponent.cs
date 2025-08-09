using Shared.ECS;

namespace Shared.Respawn
{
    /// <summary>
    /// A component that marks an entity for respawn at a specific tick.
    /// This is used on a "death record" entity.
    /// </summary>
    public class RespawnComponent : IComponent
    {
        /// <summary>
        /// The tick at which the entity should be respawned.
        /// </summary>
        public uint RespawnAtTick { get; set; }
    }
}