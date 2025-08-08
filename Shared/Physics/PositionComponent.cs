using System.Numerics;
using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Stores the 3D position of an entity.
    /// </summary>
    public class PositionComponent : IComponent
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }

        [JsonIgnore]
        public Vector3 Value
        {
            get => new(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public PositionComponent()
        {
        }

        public PositionComponent(Vector3 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }
    }
}