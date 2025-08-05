using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.Input
{
    /// <summary>
    /// Represents a player movement input message sent from the client to the server.
    /// Contains the player's intended movement direction as a 2D vector.
    /// This message is used for networked movement prediction and reconciliation.
    /// 
    /// <para>
    /// <b>Note:</b> The movement direction is serialized as separate X and Y float fields (MoveDirectionX, MoveDirectionY)
    /// instead of a Vector2. This is done for maximum compatibility and clarity with JSON serializers and network protocols,
    /// which may not natively support complex types like Vector2.
    /// </para>
    /// </summary>
    public class PlayerMovementMessage
    {
        /// <summary>
        /// The X component of the player's movement input (e.g., WASD or joystick direction).
        /// </summary>
        [JsonPropertyName("moveDirectionX")]
        public float MoveDirectionX { get; set; }

        /// <summary>
        /// The Y component of the player's movement input (e.g., WASD or joystick direction).
        /// </summary>
        [JsonPropertyName("moveDirectionY")]
        public float MoveDirectionY { get; set; }

        /// <summary>
        /// The tick count of the client when this input was generated.
        /// </summary>
        [JsonPropertyName("clientTick")]
        public long ClientTick { get; set; } = 0;

        /// <summary>
        /// Gets or sets the movement direction as a <see cref="Vector2"/>.
        /// This property is not serialized; it is provided for convenience in code.
        /// </summary>
        [JsonIgnore]
        public Vector2 MoveDirection
        {
            get => new(MoveDirectionX, MoveDirectionY);
            set
            {
                MoveDirectionX = value.X;
                MoveDirectionY = value.Y;
            }
        }
    }
}