using System;
using System.Numerics;
using LiteNetLib.Utils;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// An adapter that implements the <see cref="IComponentReader"/> interface
    /// by wrapping LiteNetLib's <see cref="NetDataReader"/>. This class acts as a bridge
    /// between the abstract component deserialization and the concrete networking library.
    /// </summary>
    public class NetDataReaderAdapter : IComponentReader
    {
        private readonly NetDataReader _reader;
        private readonly IComponentSerializer _serializer;

        public NetDataReaderAdapter(NetDataReader reader, IComponentSerializer serializer)
        {
            _reader = reader;
            _serializer = serializer;
        }

        public int GetInt() => _reader.GetInt();
        public uint GetUInt() => _reader.GetUInt();
        public float GetFloat() => _reader.GetFloat();
        public string GetString() => _reader.GetString();
        public bool GetBool() => _reader.GetBool();
        public Vector3 GetVector3()
        {
            return new Vector3(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());
        }

        public Quaternion GetQuaternion()
        {
            return new Quaternion(_reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat(), _reader.GetFloat());
        }

        public T GetComponent<T>() where T : IComponent
        {
            var bytes = _reader.GetBytesWithLength();
            return (T)_serializer.Deserialize(bytes);
        }

        public Guid GetGuid()
        {
            var bytes = new byte[16];
            _reader.GetBytes(bytes, 16);
            return new Guid(bytes);
        }

        public Vector3 GetVector3Q(float step)
        {
            short qx = _reader.GetShort();
            short qy = _reader.GetShort();
            short qz = _reader.GetShort();
            var scale = step;
            return new Vector3(qx * scale, qy * scale, qz * scale);
        }

        public Quaternion GetQuaternionCompressed()
        {
            byte header = _reader.GetByte();
            int maxIndex = header & 0x03;
            bool negative = ((header >> 2) & 0x01) == 1;

            short c0 = _reader.GetShort();
            short c1 = _reader.GetShort();
            short c2 = _reader.GetShort();

            const float invScale = 1.0f / 32767f;
            float f0 = c0 * invScale;
            float f1 = c1 * invScale;
            float f2 = c2 * invScale;

            // Reconstruct the largest component using unit length constraint
            float missingSquared = 1.0f - (f0 * f0 + f1 * f1 + f2 * f2);
            float missing = missingSquared > 0 ? MathF.Sqrt(missingSquared) : 0f;
            if (negative) missing = -missing;

            float x, y, z, w;
            switch (maxIndex)
            {
                case 0: x = missing; y = f0; z = f1; w = f2; break;
                case 1: x = f0; y = missing; z = f1; w = f2; break;
                case 2: x = f0; y = f1; z = missing; w = f2; break;
                default: x = f0; y = f1; z = f2; w = missing; break;
            }

            var q = new Quaternion(x, y, z, w);
            return Quaternion.Normalize(q);
        }
    }
}