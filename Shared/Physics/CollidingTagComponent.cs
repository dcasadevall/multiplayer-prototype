using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// A tag component that marks an entity as being collidable.
    /// The <see cref="CollisionSystem"/> will only consider entities that have this component
    /// in addition to a <see cref="WorldAABBComponent"/>.
    /// </summary>
    public class CollidingTagComponent : IComponent
    {
    }
}