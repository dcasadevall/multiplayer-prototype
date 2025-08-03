using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Associates an entity with a specific network client.
    /// </summary>
    public class PeerComponent : IComponent
    {
        [JsonPropertyName("peerId")]
        public int PeerId { get; set; }
        
        [JsonPropertyName("peerName")]
        public string PeerName { get; set; } = null!;
    }
}