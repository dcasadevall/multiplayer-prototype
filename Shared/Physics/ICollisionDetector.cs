using System.Collections.Generic;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Implement this interface to provide collision detection between entities.
    /// </summary>
    public interface ICollisionDetector
    {
        /// <summary>
        /// Returns true if the two entities are colliding at the current tick.
        /// </summary>
        /// <param name="firstEntity"></param>
        /// <param name="secondEntity"></param>
        /// <returns></returns>
        bool AreColliding(EntityId firstEntity, EntityId secondEntity);

        /// <summary>
        /// Returns all entities that are colliding with the given entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        List<EntityId> GetCollisionsFor(EntityId entityId);
    }
}