using System;
using System.Text.Json.Serialization;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Server assigns a unique ID to a newly connected client.
    /// This is part of the initial handshake process.
    /// </summary>
    public class ClientIdAssignmentMessage
    {
        [JsonPropertyName("clientId")]
        public Guid ClientId { get; set; }
    }
}