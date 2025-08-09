namespace Shared.Networking.Messages
{
    /// <summary>
    /// Defines the type of each message sent between server and client.
    /// Used as a prefix byte in all network packets to distinguish payload types.
    /// </summary>
    public enum MessageType : byte
    {
        /// <summary>
        /// Server sends this message immediately when a client connects.
        /// Contains the assigned peer ID and connection information.
        /// This is the first message in the handshake process.
        /// </summary>
        Connected = 0,

        /// <summary>
        /// A snapshot of the world state delta sent from the server to the client.
        /// Includes any changes to replicated entities and their components.
        /// It also includes the create and destroy events for entities.
        /// </summary>
        Delta = 1,

        /// <summary>
        /// Player movement input message sent from the client to the server.
        /// Contains the player's intended movement direction as a 2D vector.
        /// This message is used for networked movement prediction and reconciliation.
        /// </summary>
        PlayerMovement = 2,

        /// <summary>
        /// Player shot message sent from the client to the server.
        /// Contains shooting information for server validation and projectile spawning.
        /// Used for laser/projectile system with client-side prediction.
        /// </summary>
        PlayerShot = 3,
    }
}