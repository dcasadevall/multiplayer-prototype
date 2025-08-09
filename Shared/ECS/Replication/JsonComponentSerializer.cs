using System;
using System.IO;
using System.Text.Json;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Handles serialization and deserialization of components that implement <see cref="IComponent"/>.
    /// This implementation uses the component's AssemblyQualifiedName for robust cross-assembly type resolution,
    /// and System.Text.Json for serializing the component data.
    /// </summary>
    public class JsonComponentSerializer : IComponentSerializer
    {
        /// <summary>
        /// Serializes a component into a byte array. The format is [TypeNameString, JsonString].
        /// </summary>
        public byte[] Serialize(IComponent component)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            var type = component.GetType();
            writer.Write(type.AssemblyQualifiedName);
            
            var json = JsonSerializer.Serialize(component, type);
            writer.Write(json);

            return ms.ToArray();
        }

        /// <summary>
        /// Deserializes a component from a byte array.
        /// </summary>
        public IComponent Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var typeName = reader.ReadString();
            var componentType = Type.GetType(typeName, throwOnError: true);

            if (componentType == null)
                throw new Exception($"Could not find component type: {typeName}. Ensure the component exists on the client.");

            var json = reader.ReadString();
            var component = JsonSerializer.Deserialize(json, componentType);
            
            if (component == null)
                throw new Exception($"Failed to deserialize component of type: {componentType.Name}");

            return (IComponent)component;
        }
    }
}