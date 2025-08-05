using System;
using System.Text.Json;
using LiteNetLib;
using LiteNetLib.Utils;
using Shared.Logging;
using Shared.Networking.Messages;

namespace Shared.Networking
{
    /// <summary>
    /// Provides an implementation of <see cref="IMessageSender"/> using LiteNetLib for network communication.
    /// Messages are serialized to JSON format before being sent over the network.
    /// <para>
    /// Responsible for sending and broadcasting messages to peers over the network, supporting different message types and channels.
    /// Utilizes <see cref="ILogger"/> for structured logging of network events and errors.
    /// </para>
    /// <para>
    /// This implementation uses <see cref="NetManager"/> to look up peers and send messages using the appropriate delivery method.
    /// If a peer is not found, a warning is logged.
    /// </para>
    /// </summary>
    public class NetLibJsonMessageSender : IMessageSender
    {
        private readonly NetManager _netManager;
        private readonly ILogger _logger;

        public NetLibJsonMessageSender(NetManager netManager, ILogger logger)
        {
            _netManager = netManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public void BroadcastMessage<TMessage>(MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            _netManager.ConnectedPeerList.ForEach(peer => SendMessage(peer.Id, type, message, channel));
        }

        /// <inheritdoc />
        public void SendMessageToServer<TMessage>(MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            SendMessage(_netManager.FirstPeer.Id, type, message, channel);
        }

        /// <inheritdoc />
        public void SendMessage<TMessage>(int peerId, MessageType type, TMessage message, ChannelType channel = ChannelType.Unreliable)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((byte)type);

            // Serialize the data via Json
            try
            {
                var json = JsonSerializer.Serialize(message);
                var data = System.Text.Encoding.UTF8.GetBytes(json);
                _logger.Debug(LoggedFeature.Networking, $"Sending message of type {type} to peer {peerId}: {json}");
                writer.Put(data);
            }
            catch (Exception ex)
            {
                _logger.Error(LoggedFeature.Networking,
                    $"Failed to serialize message of type {typeof(TMessage).Name} to JSON: {ex.Message}");
                return;
            }

            NetPeer? peer = _netManager.GetPeerById(peerId);
            if (peer == null)
            {
                _logger.Warn(LoggedFeature.Networking, $"Failed to send message to peer {peerId}: Peer not found.");
                return;
            }

            peer.Send(writer, channel.ToDeliveryMethod());
        }
    }
}