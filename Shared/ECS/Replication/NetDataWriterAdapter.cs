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
        private readonly IComponentSerializer _componentSerializer;
        private readonly NetDataWriter _writer;

        public NetDataWriterAdapter(NetDataWriter writer, IComponentSerializer componentSerializer)
        {
            _writer = writer;
            _componentSerializer = componentSerializer;
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
            var bytes = _componentSerializer.Serialize(component);
            _writer.Put(component.GetType().Name);
            _writer.Put(bytes.Length);
            _writer.Put(bytes);
        }

        public void Put(Guid value)
        {
            _writer.Put(value.ToByteArray());
        }
    }
}