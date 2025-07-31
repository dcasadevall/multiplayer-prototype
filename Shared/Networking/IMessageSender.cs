namespace Shared.Networking
{
    /// <summary>
    /// Defines an abstraction for sending messages to network peers.
    /// <para>
    /// Implementations of <see cref="IMessageSender"/> are responsible for delivering messages to one or more peers,
    /// supporting different message types and delivery channels (e.g., reliable or unreliable).
    /// This abstraction allows the networking layer to be decoupled from the underlying transport implementation.
    /// </para>
    /// </summary>
    public interface IMessageSender
    {
        /// <summary>
        /// Broadcasts a message to all connected peers.
        /// </summary>
        /// <param name="type">The type of the message.</param>
        /// <param name="data">The message payload.</param>
        /// <param name="channel">The channel type (reliable/unreliable).</param>
        void BroadcastMessage(MessageType type, byte[] data, ChannelType channel = ChannelType.Unreliable);

        /// <summary>
        /// Sends a message to a specific peer by peer ID.
        /// </summary>
        /// <param name="peerId">The ID of the peer to send the message to.</param>
        /// <param name="type">The type of the message.</param>
        /// <param name="data">The message payload.</param>
        /// <param name="channel">The channel type (reliable/unreliable).</param>
        void SendMessage(int peerId, MessageType type, byte[] data, ChannelType channel = ChannelType.Unreliable);
    }
}
