using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Base class for all client-to-server messages.
    /// </summary>
    public abstract class ClientToServerMessage
    {
        /// <summary>
        /// The type of message for routing and deserialization.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when the message was created (for client-side prediction).
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        
        protected ClientToServerMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    /// <summary>
    /// Message sent by client when a player connects and wants to spawn.
    /// </summary>
    public class PlayerSpawnRequest : ClientToServerMessage
    {
        /// <summary>
        /// The desired spawn position for the player.
        /// </summary>
        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// Player's display name (optional).
        /// </summary>
        [JsonPropertyName("playerName")]
        public string PlayerName { get; set; } = string.Empty;
        
        public PlayerSpawnRequest()
        {
            Type = "PlayerSpawnRequest";
        }
        
        public PlayerSpawnRequest(Vector3 position, string playerName = "")
        {
            Type = "PlayerSpawnRequest";
            Position = position;
            PlayerName = playerName;
        }
    }
    
    /// <summary>
    /// Message sent by client for player movement input.
    /// </summary>
    public class PlayerMovementInput : ClientToServerMessage
    {
        /// <summary>
        /// The movement direction vector (normalized).
        /// </summary>
        [JsonPropertyName("direction")]
        public Vector3 Direction { get; set; }
        
        /// <summary>
        /// Whether the player is running/sprinting.
        /// </summary>
        [JsonPropertyName("isRunning")]
        public bool IsRunning { get; set; }
        
        public PlayerMovementInput()
        {
            Type = "PlayerMovementInput";
        }
        
        public PlayerMovementInput(Vector3 direction, bool isRunning = false)
        {
            Type = "PlayerMovementInput";
            Direction = direction;
            IsRunning = isRunning;
        }
    }
} 