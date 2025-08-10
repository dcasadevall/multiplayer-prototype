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

        private const float QuantizationStep = 0.01f; // centimeter precision in meters

        public PositionComponent()
        {
        }

        public PositionComponent(Vector3 value)
        {
            Value = value;
        }

        public void Serialize(IComponentWriter writer)
        {
            writer.PutVector3Q(Value, QuantizationStep);
        }

        public void Deserialize(IComponentReader reader)
        {
            Value = reader.GetVector3Q(QuantizationStep);
        }
    }
}