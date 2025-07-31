namespace Shared.Networking
{
    /// <summary>
    /// Defines the type of each message sent between server and client.
    /// Used as a prefix byte in all network packets to distinguish payload types.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// A full snapshot of the world state sent from the server to the client.
        /// Includes replicated entities and their components.
        /// </summary>
        Snapshot = 1,
    }
}