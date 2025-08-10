using System;
using System.Numerics;
using LiteNetLib.Utils;

namespace Shared.ECS.Replication
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
    }
}