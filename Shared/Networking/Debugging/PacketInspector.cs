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
            var deltasCount = reader.GetUShort();
            AddOrUpdate(result, "WorldDeltaHeader", reader.Position - wdmHeaderStart);

            for (var i = 0; i < deltasCount; i++)
            {
                // EntityDelta header
                var edHeaderStart = reader.Position;
                reader.SkipBytes(16); // Guid
                reader.GetBool();    // IsNew
                reader.GetBool();    // IsDestroyed
                AddOrUpdate(result, "EntityDeltaHeader", reader.Position - edHeaderStart);

                // Added/Modified Components
                var modifiedCountStart = reader.Position;
                var modifiedCount = reader.GetUShort();
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

                    // Slice a reader for the payload (length is componentPayloadSize)
                    var payloadReader = new NetDataReader(reader.RawData, reader.Position, componentPayloadSize);
                    var typeId = payloadReader.GetUShort();
                    var typeName = registry.GetType(typeId).Name;

                    // Skip past payload
                    reader.SkipBytes(componentPayloadSize);

                    var totalSizeOnStream = reader.Position - componentStartPos; // includes 2-byte length prefix
                    AddOrUpdate(result, typeName, totalSizeOnStream);
                }

                // Removed Components (as compact IDs)
                var removedCountStart = reader.Position;
                var removedCount = reader.GetUShort();
                AddOrUpdate(result, "RemovedCount", reader.Position - removedCountStart);

                for (var j = 0; j < removedCount; j++)
                {
                    var removedStart = reader.Position;
                    var removedId = reader.GetUShort();
                    var removedTypeName = registry.GetType(removedId).Name;
                    AddOrUpdate(result, $"Removed:{removedTypeName}", reader.Position - removedStart);
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