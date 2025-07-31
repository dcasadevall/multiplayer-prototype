using LiteNetLib;
using LiteNetLib.Utils;
using Shared.Logging;

namespace Shared.Networking
{
    /// <summary>
    /// Provides an implementation of <see cref="IMessageSender"/> using LiteNetLib for network communication.
    /// <para>
    /// Responsible for sending and broadcasting messages to peers over the network, supporting different message types and channels.
    /// Utilizes <see cref="ILogger"/> for structured logging of network events and errors.
    /// </para>
    /// <para>
    /// This implementation uses <see cref="NetManager"/> to look up peers and send messages using the appropriate delivery method.
    /// If a peer is not found, a warning is logged.
    /// </para>
    /// </summary>
    public class NetLibMessageSender : IMessageSender
    {
        private readonly NetManager _netManager;
        private readonly ILogger _logger;

        public NetLibMessageSender(NetManager netManager, ILogger logger)
        {
            _netManager = netManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public void BroadcastMessage(MessageType type, byte[] data, ChannelType channel = ChannelType.Unreliable)
        {
            _netManager.ConnectedPeerList.ForEach(peer => SendMessage(peer.Id, type, data, channel));
        }

        /// <inheritdoc />
        public void SendMessage(int peerId, MessageType type, byte[] data, ChannelType channel = ChannelType.Unreliable)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((byte)type);
            writer.Put(data);

            NetPeer? peer = _netManager.GetPeerById(peerId);
            if (peer == null)
            {
                _logger.Warn($"Failed to send message to peer {peerId}: Peer not found.");
                return;
            }
        
            peer.Send(writer, channel.ToDeliveryMethod());
        }
    }
}
