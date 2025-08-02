using System;
using System.Collections.Generic;
using System.Text.Json;
using LiteNetLib;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;

namespace Server.Networking
{
    /// <summary>
    /// Manages client connections on the server side, including the handshake process.
    /// </summary>
    public class ServerConnectionManager
    {
        private readonly ILogger _logger;
        private readonly IMessageSender _messageSender;
        private readonly Dictionary<int, Guid> _peerToClientId = new();
        private readonly Dictionary<Guid, int> _clientIdToPeer = new();

        public ServerConnectionManager(ILogger logger, IMessageSender messageSender)
        {
            _logger = logger;
            _messageSender = messageSender;
        }

        /// <summary>
        /// Called when a new client connects. Initiates the handshake process.
        /// </summary>
        public void HandleClientConnected(NetPeer peer)
        {
            var clientId = Guid.NewGuid();
            _peerToClientId[peer.Id] = clientId;
            _clientIdToPeer[clientId] = peer.Id;
            
            _logger.Info("Client connected. Peer ID: {0}, Assigned Client ID: {1}", peer.Id, clientId);
            
            // Send client ID assignment message
            var message = new ClientIdAssignmentMessage { ClientId = clientId };
            var json = JsonSerializer.SerializeToUtf8Bytes(message);
            _messageSender.SendMessage(peer.Id, MessageType.ClientIdAssignment, json);
        }

        /// <summary>
        /// Called when a client disconnects. Cleans up the client mappings.
        /// </summary>
        public void HandleClientDisconnected(NetPeer peer)
        {
            if (_peerToClientId.TryGetValue(peer.Id, out var clientId))
            {
                _logger.Info("Client disconnected. Peer ID: {0}, Client ID: {1}", peer.Id, clientId);
                _peerToClientId.Remove(peer.Id);
                _clientIdToPeer.Remove(clientId);
            }
        }

        /// <summary>
        /// Gets the client ID for a given peer ID.
        /// </summary>
        public Guid? GetClientId(int peerId)
        {
            return _peerToClientId.TryGetValue(peerId, out var clientId) ? clientId : null;
        }

        /// <summary>
        /// Gets the peer ID for a given client ID.
        /// </summary>
        public int? GetPeerId(Guid clientId)
        {
            return _clientIdToPeer.TryGetValue(clientId, out var peerId) ? peerId : null;
        }
    }
}