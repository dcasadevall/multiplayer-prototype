using System.Collections.Generic;
using LiteNetLib.Utils;
using Shared.ECS.Replication;

namespace Shared.Networking.Debugging
{
    /// <summary>
    /// A utility class for inspecting the contents of network packets.
    /// This is useful for debugging and optimizing packet sizes.
    /// </summary>
    public static class PacketInspector
    {
        /// <summary>
        /// Inspects the contents of a <see cref="NetDataWriter"/> containing a <see cref="WorldDeltaMessage"/>
        /// and returns a dictionary mapping the names of the serialized component types to their total sizes in bytes.
        /// </summary>
        /// <param name="writer">The writer containing the serialized WorldDeltaMessage.</param>
        /// <param name="registry">The component type registry to resolve component names.</param>
        /// <returns>A dictionary of component type names and their total aggregated sizes.</returns>
        public static Dictionary<string, int> Inspect(NetDataWriter writer, ComponentTypeRegistry registry)
        {
            var result = new Dictionary<string, int>();
            var reader = new NetDataReader(writer);

            // WorldDeltaMessage
            var wdmHeaderStart = reader.Position;
            var deltasCount = reader.GetInt();
            AddOrUpdate(result, "WorldDeltaHeader", reader.Position - wdmHeaderStart);

            for (var i = 0; i < deltasCount; i++)
            {
                // EntityDelta header
                var edHeaderStart = reader.Position;
                reader.SkipBytes(16); // Guid
                reader.GetBool(); // IsNew
                reader.GetBool(); // IsDestroyed
                AddOrUpdate(result, "EntityDeltaHeader", reader.Position - edHeaderStart);

                // Added/Modified Components
                var modifiedCountStart = reader.Position;
                var modifiedCount = reader.GetInt();
                AddOrUpdate(result, "ModifiedCount", reader.Position - modifiedCountStart);

                for (var j = 0; j < modifiedCount; j++)
                {
                    var componentStartPos = reader.Position;
                    var componentPayloadSize = reader.GetUShort(); // From PutBytesWithLength

                    if (reader.AvailableBytes < componentPayloadSize)
                    {
                        AddOrUpdate(result, "MalformedComponent", reader.AvailableBytes);
                        return result;
                    }

                    var payloadReader = new NetDataReader(reader.RawData, reader.Position, reader.Position + componentPayloadSize);
                    var typeId = payloadReader.GetUShort();
                    var typeName = registry.GetType(typeId).Name;

                    reader.SkipBytes(componentPayloadSize);

                    var totalSizeOnStream = reader.Position - componentStartPos;
                    AddOrUpdate(result, typeName, totalSizeOnStream);
                }

                // Removed Components
                var removedCountStart = reader.Position;
                var removedCount = reader.GetInt();
                AddOrUpdate(result, "RemovedCount", reader.Position - removedCountStart);

                for (var j = 0; j < removedCount; j++)
                {
                    var removedStart = reader.Position;
                    reader.GetString(); // AssemblyQualifiedName
                    AddOrUpdate(result, "RemovedComponent", reader.Position - removedStart);
                }
            }

            return result;
        }

        private static void AddOrUpdate(Dictionary<string, int> dict, string key, int value)
        {
            if (dict.ContainsKey(key))
                dict[key] += value;
            else
                dict.Add(key, value);
        }
    }
}