using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.ECS.TickSync
{
    /// <summary>
    /// Component that holds the current server tick number.
    /// Used by the <see cref="ServerTickSystem"/> to provide the server tick state
    /// to clients for synchronization purposes.
    /// </summary>
    public class ServerTickComponent : IComponent
    {
        /// <summary>
        /// The tick number for the server.
        /// </summary>
        public uint TickNumber { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(TickNumber);
        }

        public void Deserialize(IComponentReader reader)
        {
            TickNumber = reader.GetUInt();
        }
    }
}