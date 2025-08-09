using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.IO;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Handles serialization and deserialization of components that implement <see cref="IComponent"/>.
    /// This class uses reflection to discover all component types at startup and maps them to a unique ID,
    /// which is used for efficient network transport.
    /// </summary>
    public class JsonComponentSerializer : IComponentSerializer
    {
        private readonly Dictionary<Type, byte> _componentIds = new();
        private readonly Dictionary<byte, Type> _idToType = new();

        /// <summary>
        /// Constructs the serializer by scanning for all component types.
        /// </summary>
        public JsonComponentSerializer()
        {
            var componentTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsInterface);

            byte id = 0;
            foreach (var type in componentTypes)
            {
                _componentIds[type] = id;
                _idToType[id] = type;
                id++;
            }
        }

        /// <summary>
        /// Serializes a component into a NetDataWriter.
        /// </summary>
        public byte[] Serialize(IComponent component)
        {
            var type = component.GetType();
            if (!_componentIds.TryGetValue(type, out var id))
            {
                throw new Exception($"Unknown component type: {type.Name}");
            }

            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);

            writer.Write(id);
            var json = JsonSerializer.Serialize(component, type);
            writer.Write(json);

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Deserializes a component from a NetDataReader.
        /// </summary>
        public IComponent Deserialize(byte[] data)
        {
            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream);

            var id = reader.ReadByte();
            if (!_idToType.TryGetValue(id, out var type))
            {
                throw new Exception($"Unknown component ID: {id}");
            }

            var json = reader.ReadString();
            var component = JsonSerializer.Deserialize(json, type);
            if (component == null)
            {
                throw new Exception($"Failed to deserialize component of type: {type.Name}");
            }

            return (IComponent)component;
        }
    }
}