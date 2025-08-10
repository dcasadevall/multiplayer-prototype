using System;
using System.Numerics;
using LiteNetLib.Utils;
using Shared.ECS;
using Shared.Math;

namespace Shared.Replication
{
    /// <summary>
    /// An adapter that implements the <see cref="IComponentWriter"/> interface
    /// by wrapping LiteNetLib's <see cref="NetDataWriter"/>. This class acts as a bridge
    /// between the abstract component serialization and the concrete networking library.
    /// </summary>
    public class NetDataWriterAdapter : IComponentWriter
    {
        private readonly NetDataWriter _writer;
        private readonly IComponentSerializer _serializer;

        public NetDataWriterAdapter(NetDataWriter writer, IComponentSerializer serializer)
        {
            _writer = writer;
            _serializer = serializer;
        }

        public void Put(int value) => _writer.Put(value);
        public void Put(uint value) => _writer.Put(value);
        public void Put(float value) => _writer.Put(value);
        public void Put(string value) => _writer.Put(value);
        public void Put(bool value) => _writer.Put(value);

        public void Put(Vector3 value)
        {
            _writer.Put(value.X);
            _writer.Put(value.Y);
            _writer.Put(value.Z);
        }

        public void Put(Quaternion value)
        {
            _writer.Put(value.X);
            _writer.Put(value.Y);
            _writer.Put(value.Z);
            _writer.Put(value.W);
        }

        public void Put(IComponent component)
        {
            var bytes = _serializer.Serialize(component);
            _writer.PutBytesWithLength(bytes);
        }

        public void Put(Guid value)
        {
            _writer.Put(value.ToByteArray());
        }

        public void PutVector3Q(Vector3 value, float step)
        {
            var inv = 1.0f / step;
            short qx = (short)Clamping.Clamp((int)MathF.Round(value.X * inv), short.MinValue, short.MaxValue);
            short qy = (short)Clamping.Clamp((int)MathF.Round(value.Y * inv), short.MinValue, short.MaxValue);
            short qz = (short)Clamping.Clamp((int)MathF.Round(value.Z * inv), short.MinValue, short.MaxValue);
            _writer.Put(qx);
            _writer.Put(qy);
            _writer.Put(qz);
        }

        public void PutQuaternionCompressed(Quaternion value)
        {
            // Ensure unit quaternion
            value = Quaternion.Normalize(value);
            var x = value.X;
            var y = value.Y;
            var z = value.Z;
            var w = value.W;

            // Find largest component index (0..3)
            int maxIndex = 0;
            float maxAbs = MathF.Abs(x);
            if (MathF.Abs(y) > maxAbs)
            {
                maxAbs = MathF.Abs(y);
                maxIndex = 1;
            }

            if (MathF.Abs(z) > maxAbs)
            {
                maxAbs = MathF.Abs(z);
                maxIndex = 2;
            }

            if (MathF.Abs(w) > maxAbs)
            {
                maxAbs = MathF.Abs(w);
                maxIndex = 3;
            }

            // Make largest positive (q and -q are equivalent)
            switch (maxIndex)
            {
                case 0:
                    if (x < 0)
                    {
                        x = -x;
                        y = -y;
                        z = -z;
                        w = -w;
                    }

                    break;
                case 1:
                    if (y < 0)
                    {
                        x = -x;
                        y = -y;
                        z = -z;
                        w = -w;
                    }

                    break;
                case 2:
                    if (z < 0)
                    {
                        x = -x;
                        y = -y;
                        z = -z;
                        w = -w;
                    }

                    break;
                case 3:
                    if (w < 0)
                    {
                        x = -x;
                        y = -y;
                        z = -z;
                        w = -w;
                    }

                    break;
            }

            // Store the other three components scaled to Int16 range
            Span<float> comps = stackalloc float[3];
            int idx = 0;
            if (maxIndex != 0) comps[idx++] = x;
            if (maxIndex != 1) comps[idx++] = y;
            if (maxIndex != 2) comps[idx++] = z;
            if (maxIndex != 3) comps[idx++] = w;

            const float scale = 32767f; // map [-1,1] -> [-32767,32767]
            short c0 = (short)Clamping.Clamp((int)MathF.Round(comps[0] * scale), short.MinValue, short.MaxValue);
            short c1 = (short)Clamping.Clamp((int)MathF.Round(comps[1] * scale), short.MinValue, short.MaxValue);
            short c2 = (short)Clamping.Clamp((int)MathF.Round(comps[2] * scale), short.MinValue, short.MaxValue);

            // Header byte: 2 bits for index (0..3)
            byte header = (byte)(maxIndex & 0x03);
            _writer.Put(header);
            _writer.Put(c0);
            _writer.Put(c1);
            _writer.Put(c2);
        }
    }
}