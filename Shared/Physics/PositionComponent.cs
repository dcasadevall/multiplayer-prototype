using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.Physics
{
    /// <summary>
    /// Stores the 3D position of an entity.
    /// </summary>
    public class PositionComponent : IComponent
    {
        public Vector3 Value { get; set; }

        public PositionComponent()
        {
        }

        public PositionComponent(Vector3 value)
        {
            Value = value;
        }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(Value);
        }

        public void Deserialize(IComponentReader reader)
        {
            Value = reader.GetVector3();
        }
    }
}