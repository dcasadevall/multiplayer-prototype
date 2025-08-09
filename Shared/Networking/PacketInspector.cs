using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Shared.Networking
{
    /// <summary>
    /// A utility class for inspecting the contents of network packets.
    /// This is useful for debugging and optimizing packet sizes.
    /// </summary>
    public static class PacketInspector
    {
        /// <summary>
        /// Inspects the contents of a <see cref="NetDataWriter"/> and returns a dictionary
        /// mapping the names of the serialized types to their sizes in bytes.
        /// </summary>
        /// <param name="writer">The writer to inspect.</param>
        /// <returns>A dictionary of type names and their sizes.</returns>
        public static Dictionary<string, int> Inspect(NetDataWriter writer)
        {
            var result = new Dictionary<string, int>();
            var reader = new NetDataReader(writer);
            while (!reader.EndOfData)
            {
                var typeName = reader.GetString();
                var size = reader.GetInt();
                if (result.ContainsKey(typeName))
                    result[typeName] += size;
                else
                    result[typeName] = size;
                reader.SkipBytes(size);
            }
            return result;
        }
    }
}
