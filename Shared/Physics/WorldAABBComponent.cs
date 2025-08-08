using System.Numerics;
using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Represents the axis-aligned bounding box (AABB) of an entity in world space.
    /// This is often calculated by a system based on other components like Position and a collider shape.
    /// </summary>
    public class WorldAABBComponent : IComponent
    {
        [JsonIgnore]
        public Vector3 Min { get; set; }
        [JsonIgnore]
        public Vector3 Max { get; set; }

        [JsonPropertyName("minx")]
        public float MinX { get => Min.X; set => Min = new Vector3(value, Min.Y, Min.Z); }
        [JsonPropertyName("miny")]
        public float MinY { get => Min.Y; set => Min = new Vector3(Min.X, value, Min.Z); }
        [JsonPropertyName("minz")]
        public float MinZ { get => Min.Z; set => Min = new Vector3(Min.X, Min.Y, value); }
        
        [JsonPropertyName("maxx")]
        public float MaxX { get => Max.X; set => Max = new Vector3(value, Max.Y, Max.Z); }
        [JsonPropertyName("maxy")]
        public float MaxY { get => Max.Y; set => Max = new Vector3(Max.X, value, Max.Z); }
        [JsonPropertyName("maxz")]
        public float MaxZ { get => Max.Z; set => Max = new Vector3(Max.X, Max.Y, value); }
    }
}

