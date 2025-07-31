using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Base class for all server-to-client messages.
    /// </summary>
    public abstract class ServerToClientMessage
    {
        /// <summary>
        /// The type of message for routing and deserialization.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when the message was created on the server.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
        
        protected ServerToClientMessage()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
    
    /// <summary>
    /// Response to a player spawn request, confirming the player was created.
    /// </summary>
    public class PlayerSpawnResponse : ServerToClientMessage
    {
        /// <summary>
        /// The unique ID assigned to the player entity.
        /// </summary>
        [JsonPropertyName("playerEntityId")]
        public Guid PlayerEntityId { get; set; }
        
        /// <summary>
        /// The final spawn position (may differ from requested position).
        /// </summary>
        [JsonPropertyName("spawnPosition")]
        public Vector3 SpawnPosition { get; set; }
        
        /// <summary>
        /// Whether the spawn was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if spawn failed.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
        
        public PlayerSpawnResponse()
        {
            Type = "PlayerSpawnResponse";
        }
        
        public PlayerSpawnResponse(Guid playerEntityId, Vector3 spawnPosition, bool success = true, string errorMessage = "")
        {
            Type = "PlayerSpawnResponse";
            PlayerEntityId = playerEntityId;
            SpawnPosition = spawnPosition;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
    
    /// <summary>
    /// Message sent when a player entity is destroyed (disconnect, death, etc.).
    /// </summary>
    public class PlayerDestroyedMessage : ServerToClientMessage
    {
        /// <summary>
        /// The ID of the player entity that was destroyed.
        /// </summary>
        [JsonPropertyName("playerEntityId")]
        public Guid PlayerEntityId { get; set; }
        
        /// <summary>
        /// Reason for the destruction (optional).
        /// </summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
        
        public PlayerDestroyedMessage()
        {
            Type = "PlayerDestroyedMessage";
        }
        
        public PlayerDestroyedMessage(Guid playerEntityId, string reason = "")
        {
            Type = "PlayerDestroyedMessage";
            PlayerEntityId = playerEntityId;
            Reason = reason;
        }
    }
} 