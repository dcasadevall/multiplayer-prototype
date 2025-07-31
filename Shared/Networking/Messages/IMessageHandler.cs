using System;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Interface for handling client-to-server messages.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to handle.</typeparam>
    public interface IMessageHandler<in TMessage> where TMessage : ClientToServerMessage
    {
        /// <summary>
        /// Handles a client-to-server message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="clientId">The ID of the client that sent the message.</param>
        /// <returns>A response message to send back to the client, or null if no response is needed.</returns>
        ServerToClientMessage? HandleMessage(TMessage message, Guid clientId);
    }
} 