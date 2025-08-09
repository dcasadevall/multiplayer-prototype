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

        public NetDataReaderAdapter(NetDataReader reader)
        {
            _reader = reader;
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
            var serializer = new BinaryComponentSerializer();
            var size = _reader.GetInt();
            var bytes = new byte[size];
            _reader.GetBytes(bytes, size);
            return (T)serializer.Deserialize(bytes);
        }

        public Guid GetGuid()
        {
            var bytes = new byte[16];
            _reader.GetBytes(bytes, 16);
            return new Guid(bytes);
        }
    }
}