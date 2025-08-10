using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.Physics
{
    public class RotationComponent : IComponent
    {
        public Quaternion Value { get; set; }

        public RotationComponent()
        {
            Value = Quaternion.Identity;
        }

        public RotationComponent(Quaternion value)
        {
            Value = value;
        }

        public void Serialize(IComponentWriter writer)
        {
            writer.PutQuaternionCompressed(Value);
        }

        public void Deserialize(IComponentReader reader)
        {
            Value = reader.GetQuaternionCompressed();
        }
    }
}