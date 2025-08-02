using System;
using Shared.Networking.Messages;

namespace Shared.Networking
{
    /// <summary>
    /// Defines an abstraction for receiving messages from network peers.
    /// <para>
    /// Implementations of <see cref="IMessageReceiver"/> are responsible for receiving messages from peers,
    /// parsing message types, and notifying subscribers of incoming messages. This abstraction allows
    /// the networking layer to be decoupled from the underlying transport implementation.
    /// </para>
    /// <para>
    /// Message receivers typically provide a way to register handlers for specific message types.
    /// When a message of the registered type is received, the corresponding handler is invoked.
    /// </para>
    /// </summary>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Registers a handler for messages of type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <param name="handlerId">A unique identifier for the handler (used for deregistration).</param>
        /// <param name="handler">The action to invoke when a message of type <typeparamref name="TMessage"/> is received.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that can be used to unregister the handler when it is no longer needed.
        /// </returns>
        IDisposable RegisterMessageHandler<TMessage>(string handlerId, Action<TMessage> handler);
    }
}
