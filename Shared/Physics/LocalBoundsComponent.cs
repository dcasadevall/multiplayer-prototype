using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.Physics
{
    /// <summary>
    /// Defines the dimensions of an entity's bounds in its own local space, before any rotation or translation.
    /// This component is used by the <see cref="WorldAABBUpdateSystem"/> to calculate the world-space
    /// axis-aligned bounding box (<see cref="WorldAABBComponent"/>).
    /// </summary>
    public class LocalBoundsComponent : IComponent
    {
        public Vector3 Center { get; set; }
        public Vector3 Size { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(Center);
            writer.Put(Size);
        }

        public void Deserialize(IComponentReader reader)
        {
            Center = reader.GetVector3();
            Size = reader.GetVector3();
        }
    }
}

