using System.Numerics;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Defines the dimensions of an entity's bounds in its own local space, before any rotation or translation.
    /// This component is used by the <see cref="WorldAABBUpdateSystem"/> to calculate the world-space
    /// axis-aligned bounding box (<see cref="WorldAABBComponent"/>).
    /// </summary>
    public class LocalBoundsComponent : IComponent
    {
        /// <summary>
        /// The center of the bounds in local space.
        /// </summary>
        public Vector3 Center { get; set; }

        /// <summary>
        /// The full size of the bounds in local space.
        /// </summary>
        public Vector3 Size { get; set; }
    }
}

