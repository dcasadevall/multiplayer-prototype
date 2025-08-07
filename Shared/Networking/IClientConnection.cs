using System;

namespace Shared.Networking
{
    /// <summary>
    /// Represents an active network connection to the authoritative server.
    /// Provides access to the assigned peer ID, message sending, and message receiving interfaces.
    /// </summary>
    public interface IClientConnection : IDisposable
    {
        /// <summary>
        /// The unique peer ID assigned by the server for this connection.
        /// </summary>
        int AssignedPeerId { get; }

        /// <summary>
        /// The current ping time in milliseconds to the server.
        /// This is Rtt/2
        /// </summary>
        int PingMs { get; }

        /// <summary>
        /// The Server Tick number at the time of connection.
        /// </summary>
        uint StartingServerTick { get; }

        /// <summary>
        /// The message sender for sending messages to the server or other peers.
        /// </summary>
        IMessageSender MessageSender { get; }

        /// <summary>
        /// The message receiver for receiving messages from the server or other peers.
        /// </summary>
        IMessageReceiver MessageReceiver { get; }
    }
}