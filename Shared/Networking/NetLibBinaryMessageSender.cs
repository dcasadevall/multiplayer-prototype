using LiteNetLib;
using LiteNetLib.Utils;
using Shared.Logging;
using Shared.Networking.Messages;

namespace Shared.Networking
{
    public class NetLibBinaryMessageSender : IMessageSender
    {
        private readonly NetManager _netManager;
        private readonly ILogger _logger;

        public NetLibBinaryMessageSender(NetManager netManager, ILogger logger)
        {
            _netManager = netManager;
            _logger = logger;
        }

        public void BroadcastMessage<TMessage>(MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            _netManager.ConnectedPeerList.ForEach(peer => SendMessage(peer.Id, type, message, channel));
        }

        public void SendMessageToServer<TMessage>(MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            if (_netManager.FirstPeer == null)
            {
                _logger.Warn(LoggedFeature.Networking, "Failed to send message to server: Not connected to any peer.");
                return;
            }

            SendMessage(_netManager.FirstPeer.Id, type, message, channel);
        }

        public void SendMessage<TMessage>(int peerId, MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            if (message is not INetSerializable serializable)
            {
                _logger.Error(LoggedFeature.Networking,
                    $"Failed to send message of type {typeof(TMessage).Name}: It does not implement INetSerializable.");
                return;
            }

            NetDataWriter writer = new NetDataWriter();
            writer.Put((byte)type);

            // Serialize the data using the custom serializer
            serializable.Serialize(writer);

            NetPeer? peer = _netManager.GetPeerById(peerId);
            if (peer == null)
            {
                _logger.Warn(LoggedFeature.Networking, $"Failed to send message to peer {peerId}: Peer not found.");
                return;
            }

            NetworkStats.RecordMessageSent(writer.Length);
            peer.Send(writer, channel.ToDeliveryMethod());
        }
    }
}
