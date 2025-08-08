using System;
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
        /// Local entity ID of the predicted projectile on the client.
        /// Used for associating client prediction with server authority.
        /// </summary>
        [JsonPropertyName("predictedProjectileId")]
        public Guid PredictedProjectileId { get; set; }
    }
}