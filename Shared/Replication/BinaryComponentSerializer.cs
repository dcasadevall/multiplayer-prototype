using System;
using LiteNetLib.Utils;
using Shared.ECS;

namespace Shared.Replication
{
    /// <summary>
    /// A component serializer that uses a binary format.
    /// This class is responsible for converting components to and from byte arrays,
    /// and it uses the <see cref="NetDataWriterAdapter"/> and <see cref="NetDataReaderAdapter"/>
    /// to abstract away the details of the underlying networking library.
    /// </summary>
    public class BinaryComponentSerializer : IComponentSerializer
    {
        private readonly ComponentTypeRegistry _componentTypeRegistry;

        public BinaryComponentSerializer(ComponentTypeRegistry componentTypeRegistry)
        {
            _componentTypeRegistry = componentTypeRegistry;
        }

        public byte[] Serialize(IComponent component)
        {
            var writer = new NetDataWriter();
            var typeId = _componentTypeRegistry.GetId(component.GetType());
            writer.Put(typeId);
            component.Serialize(new NetDataWriterAdapter(writer, this));
            return writer.CopyData();
        }

        public IComponent Deserialize(byte[] data)
        {
            var reader = new NetDataReader(data);
            var typeId = reader.GetUShort();
            var type = _componentTypeRegistry.GetType(typeId);
            var component = (IComponent)Activator.CreateInstance(type);
            component.Deserialize(new NetDataReaderAdapter(reader, this));
            return component;
        }
    }
}