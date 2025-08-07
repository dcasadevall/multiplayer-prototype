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
    }
}