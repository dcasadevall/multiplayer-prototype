using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.Input
{
    /// <summary>
    /// Message sent from client to server when the player shoots.
    /// Contains shooting information for server validation and projectile spawning.
    /// </summary>
    public class PlayerShotMessage
    {
        /// <summary>
        /// The tick at which the shot was fired on the client.
        /// </summary>
        [JsonPropertyName("tick")]
        public uint Tick { get; set; }

        /// <summary>
        /// The X component of the direction in which the shot was fired.
        /// </summary>
        [JsonPropertyName("directionX")]
        public float DirectionX { get; set; }

        /// <summary>
        /// The Z component of the direction in which the shot was fired.
        /// </summary>
        [JsonPropertyName("fireDirectionZ")]
        public float DirectionZ { get; set; }

        /// <summary>
        /// The direction in which the shot was fired (normalized vector).
        /// </summary>
        [JsonIgnore]
        public Vector2 FireDirection
        {
            get => new Vector2(DirectionX, DirectionZ);
            set
            {
                DirectionX = value.X;
                DirectionZ = value.Y;
            }
        }

        /// <summary>
        /// Local entity ID of the predicted projectile on the client.
        /// Used for associating client prediction with server authority.
        /// </summary>
        [JsonPropertyName("predictedProjectileId")]
        public Guid PredictedProjectileId { get; set; }
    }
}