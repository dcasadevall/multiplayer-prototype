using System;
using System.Text.Json.Serialization;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Message sent by the server immediately when a client connects.
    /// Contains the assigned peer ID that the client must use for all subsequent communication.
    /// This is the first message in the handshake process.
    /// </summary>
    public class ConnectedMessage
    {
        /// <summary>
        /// The peer ID assigned to the client by the server.
        /// This ID must be used for all subsequent client-to-server messages.
        /// </summary>
        [JsonPropertyName("peerId")]
        public int PeerId { get; set; }

        /// <summary>
        /// Timestamp when the connection was established (server time).
        /// </summary>
        [JsonPropertyName("connectionTime")]
        public DateTime ConnectionTime { get; set; }

        /// <summary>
        /// Server version information for compatibility checking.
        /// </summary>
        [JsonPropertyName("serverVersion")]
        public string ServerVersion { get; set; } = "1.0.0";
    }
}