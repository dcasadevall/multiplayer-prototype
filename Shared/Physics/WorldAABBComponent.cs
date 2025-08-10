using System.Numerics;
using Shared.ECS;
using Shared.Replication;

namespace Shared.Physics
{
    /// <summary>
    /// Represents the axis-aligned bounding box (AABB) of an entity in world space.
    /// This is often calculated by a system based on other components like Position and a collider shape.
    /// </summary>
    public class WorldAABBComponent : INonReplicatedComponent
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(Min);
            writer.Put(Max);
        }

        public void Deserialize(IComponentReader reader)
        {
            Min = reader.GetVector3();
            Max = reader.GetVector3();
        }
    }
}