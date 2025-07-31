using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Shared.Logging;
using Shared.Networking;

namespace Server.Networking
{
    /// <summary>
    /// Implements <see cref="INetEventListener"/> to broadcast LiteNetLib network events to subscribers.
    /// Provides structured logging for connection, disconnection, errors, and message receipt.
    /// </summary>
    public class NetEventBroadcaster : INetEventListener
    {
        /// <summary>
        /// Raised when a peer connects to the server.
        /// </summary>
        public event Action<NetPeer>? PeerConnected;

        /// <summary>
        /// Raised when a peer disconnects from the server.
        /// </summary>
        public event Action<NetPeer, DisconnectInfo>? PeerDisconnected;

        private readonly ILogger _logger;

        /// <summary>
        /// Constructs a new <see cref="NetEventBroadcaster"/> with the given logger.
        /// </summary>
        /// <param name="logger">Structured logger for network events.</param>
        public NetEventBroadcaster(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void OnPeerConnected(NetPeer peer)
        {
            _logger.Info("Peer connected: {0} with ID: {1}", peer.Address, peer.Id);
            PeerConnected?.Invoke(peer);
        }

        /// <inheritdoc />
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            _logger.Info("Peer disconnected: {0} ({1})", peer.Address, disconnectInfo.Reason);
            PeerDisconnected?.Invoke(peer, disconnectInfo);
        }

        /// <inheritdoc />
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            _logger.Error("Network error: {0} at {1}", socketError, endPoint);
        }

        /// <inheritdoc />
        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            var messageType = (MessageType)reader.GetByte();
            _logger.Warn("Unhandled message received from {0}: {1}", peer.Address, messageType);
            reader.Recycle();
        }

        /// <inheritdoc />
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // No-op for unconnected messages
        }

        /// <inheritdoc />
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Optionally log or broadcast latency updates
        }

        /// <inheritdoc />
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }
    }
}