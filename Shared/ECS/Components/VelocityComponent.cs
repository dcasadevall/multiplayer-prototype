using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    ///     Stores the 3D velocity of an entity.
    /// </summary>
    public class VelocityComponent : IComponent
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
            get => new Vector3(X, Y, Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }
    
        public VelocityComponent() { }
    
        public VelocityComponent(Vector3 value)
        {
            X = value.X;
            Y = value.Y;
            Z = value.Z;
        }
    }
}