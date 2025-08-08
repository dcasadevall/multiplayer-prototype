using System.Text.Json.Serialization;
using Shared.ECS;

namespace Shared.Player
{
    /// <summary>
    /// Component that marks a player as dead and tracks death tick and peer ID.
    /// Used for respawn logic and death tracking.
    /// </summary>
    public class DeadPlayerComponent : IComponent
    {
        /// <summary>
        /// The tick number at which the player died.
        /// </summary>
        [JsonPropertyName("diedAtTick")]
        public uint DiedAtTick { get; set; }

        /// <summary>
        /// The peer ID of the player who died.
        /// </summary>
        [JsonPropertyName("peerId")]
        public int PeerId { get; set; }
    }
}