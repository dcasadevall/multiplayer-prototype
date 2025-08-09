using System;
using LiteNetLib.Utils;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// A component serializer that uses a binary format.
    /// This class is responsible for converting components to and from byte arrays,
    /// and it uses the <see cref="NetDataWriterAdapter"/> and <see cref="NetDataReaderAdapter"/>
    /// to abstract away the details of the underlying networking library.
    /// </summary>
    public class BinaryComponentSerializer : IComponentSerializer
    {
        public byte[] Serialize(IComponent component)
        {
            var writer = new NetDataWriter();
            writer.Put(component.GetType().AssemblyQualifiedName);
            component.Serialize(new NetDataWriterAdapter(writer));
            return writer.CopyData();
        }

        public IComponent Deserialize(byte[] data)
        {
            var reader = new NetDataReader(data);
            var typeName = reader.GetString();
            var type = Type.GetType(typeName);
            var component = (IComponent)Activator.CreateInstance(type);
            component.Deserialize(new NetDataReaderAdapter(reader));
            return component;
        }
    }
}
