using System.Numerics;
using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Physics
{
    public class RotationComponent : IComponent
    {
        [JsonIgnore]
        public Quaternion Value { get; set; }

        [JsonPropertyName("x")]
        public float X
        {
            get => Value.X;
            set => Value = new Quaternion(value, Value.Y, Value.Z, Value.W);
        }

        [JsonPropertyName("y")]
        public float Y
        {
            get => Value.Y;
            set => Value = new Quaternion(Value.X, value, Value.Z, Value.W);
        }

        [JsonPropertyName("z")]
        public float Z
        {
            get => Value.Z;
            set => Value = new Quaternion(Value.X, Value.Y, value, Value.W);
        }

        [JsonPropertyName("w")]
        public float W
        {
            get => Value.W;
            set => Value = new Quaternion(Value.X, Value.Y, Value.Z, value);
        }

        public RotationComponent()
        {
            Value = Quaternion.Identity;
        }

        public RotationComponent(Quaternion value)
        {
            Value = value;
        }
    }
}