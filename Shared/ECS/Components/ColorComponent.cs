using System;
using System.Drawing;
using Shared.Replication;

namespace Shared.ECS.Components
{
    /// <summary>
    /// A color component without alpha channel, used for entities that need a color representation.
    /// </summary>
    public class ColorComponent : IComponent
    {
        private static readonly Random Random = new();

        public Color Value { get; set; }

        public static ColorComponent RandomColor()
        {
            // Generate a random color with full opacity
            return new ColorComponent
            {
                Value = Color.FromArgb(Random.Next(0, 256), Random.Next(0, 256), Random.Next(0, 256)),
            };
        }

        public void Serialize(IComponentWriter writer)
        {
            writer.PutByte(Value.R);
            writer.PutByte(Value.G);
            writer.PutByte(Value.B);
        }

        public void Deserialize(IComponentReader reader)
        {
            var r = reader.GetByte();
            var g = reader.GetByte();
            var b = reader.GetByte();
            Value = Color.FromArgb(r, g, b);
        }
    }
}