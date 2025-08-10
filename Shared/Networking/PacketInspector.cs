using System.Collections.Generic;
using LiteNetLib.Utils;
using Shared.ECS.Replication;

namespace Shared.Networking
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
            var result = new Dictionary<string, int> { { "Header", 0 } };
            if (registry == null)
            {
                result["Error"] = "ComponentTypeRegistry is null".Length;
                return result;
            }

            var reader = new NetDataReader(writer);

            // WorldDeltaMessage header
            var startPos = reader.Position;
            reader.GetUInt(); // Tick
            var entityDeltasCount = reader.GetUShort();
            result["Header"] = reader.Position - startPos;


            for (var i = 0; i < entityDeltasCount; i++)
            {
                // EntityDelta header
                startPos = reader.Position;
                reader.GetUInt(); // EntityId
                var componentsCount = reader.GetByte();
                var deltaHeaderSize = reader.Position - startPos;
                if(result.ContainsKey("EntityDelta"))
                    result["EntityDelta"] += deltaHeaderSize;
                else
                    result.Add("EntityDelta", deltaHeaderSize);


                // Modified Components
                for (var j = 0; j < componentsCount; j++)
                {
                    startPos = reader.Position;
                    var typeId = reader.GetUShort();
                    var typeName = registry.GetType(typeId).Name;
                    var componentDataSize = reader.GetUShort(); // Size from PutBytesWithLength
                    reader.SkipBytes(componentDataSize);
                    var totalComponentSize = reader.Position - startPos;

                    if (result.ContainsKey(typeName))
                        result[typeName] += totalComponentSize;
                    else
                        result.Add(typeName, totalComponentSize);
                }
            }

            return result;
        }
    }
}
