using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.Input
{
    /// <summary>
    /// Represents a player movement input message sent from the client to the server.
    /// Contains the player's intended movement direction as a 2D vector.
    /// This message is used for networked movement prediction and reconciliation.
    /// </summary>
    public class PlayerMovementMessage
    {
        /// <summary>
        /// The player's movement input as a normalized 2D vector (e.g., WASD or joystick direction).
        /// </summary>
        [JsonPropertyName("moveDirection")]
        public Vector2 MoveDirection { get; set; } = Vector2.Zero;

        /// <summary>
        /// The tick count of the client when this input was generated.
        /// </summary>
        [JsonPropertyName("clientTick")]
        public long ClientTick { get; set; } = 0;
    }
}