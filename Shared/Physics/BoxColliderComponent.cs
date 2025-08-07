using System.Numerics;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Entities containing this component will be treated as having a box collider.
    /// They will be used to detect collisions between entities.
    /// </summary>
    public class BoxColliderComponent : IComponent
    {
        /// <summary>
        /// The center of the box collider relative to the containing entity.
        /// </summary>
        public Vector3 Center { get; set; } = Vector3.Zero;

        /// <summary>
        /// The size of the box collider, relative to the containing entity.
        /// </summary>
        public Vector3 Size { get; set; } = Vector3.One;
    }
}