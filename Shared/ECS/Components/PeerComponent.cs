using Shared.ECS;
using Shared.Replication;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Associates an entity with a specific network client.
    /// </summary>
    public class PeerComponent : IComponent
    {
        public int PeerId { get; set; }
        public string PeerName { get; set; } = null!;

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(PeerId);
            writer.Put(PeerName);
        }

        public void Deserialize(IComponentReader reader)
        {
            PeerId = reader.GetInt();
            PeerName = reader.GetString();
        }
    }
}