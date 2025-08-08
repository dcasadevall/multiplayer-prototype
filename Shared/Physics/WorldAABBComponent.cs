using System.Numerics;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Represents the axis-aligned bounding box (AABB) of an entity in world space.
    /// This is often calculated by a system based on other components like Position and a collider shape.
    /// </summary>
    public class WorldAABBComponent : IComponent
    {
        /// <summary>
        /// The minimum corner of the bounding box.
        /// </summary>
        public Vector3 Min { get; set; }

        /// <summary>
        /// The maximum corner of the bounding box.
        /// </summary>
        public Vector3 Max { get; set; }
    }
}

