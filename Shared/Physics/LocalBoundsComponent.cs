using System.Numerics;
using System.Text.Json.Serialization;
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
        [JsonIgnore]
        public Vector3 Center { get; set; }
        [JsonIgnore]
        public Vector3 Size { get; set; }

        [JsonPropertyName("cx")]
        public float CenterX { get => Center.X; set => Center = new Vector3(value, Center.Y, Center.Z); }
        [JsonPropertyName("cy")]
        public float CenterY { get => Center.Y; set => Center = new Vector3(Center.X, value, Center.Z); }
        [JsonPropertyName("cz")]
        public float CenterZ { get => Center.Z; set => Center = new Vector3(Center.X, Center.Y, value); }
        
        [JsonPropertyName("sx")]
        public float SizeX { get => Size.X; set => Size = new Vector3(value, Size.Y, Size.Z); }
        [JsonPropertyName("sy")]
        public float SizeY { get => Size.Y; set => Size = new Vector3(Size.X, value, Size.Z); }
        [JsonPropertyName("sz")]
        public float SizeZ { get => Size.Z; set => Size = new Vector3(Size.X, Size.Y, value); }
    }
}

