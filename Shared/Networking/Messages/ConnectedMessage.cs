using System.Text.Json.Serialization;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Message sent back to the client on connection.
    /// Clients should not consider a connection complete until this message
    /// is received
    /// </summary>
    public class ConnectedMessage
    {
        [JsonPropertyName("assignedPeerId")] public int AssignedPeerId { get; set; }
    }
}