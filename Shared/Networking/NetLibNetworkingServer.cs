using System;
using System.Net;
using System.Threading;
using LiteNetLib;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking.Messages;
using Shared.Scheduling;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using System.Linq;

namespace Shared.Networking
{
    /// <summary>
    /// An implementation of <see cref="INetworkingServer"/> using LiteNetLib for networking.
    /// This server handles the protocol for accepting a connection and sending back
    /// the AssignedClientId message type to the client upon connection.
    /// <para>
    /// This server manages a <see cref="NetManager"/> instance, handles connection requests,
    /// logs incoming messages, and manages the server loop and shutdown.
    /// </para>
    /// <para>
    /// All network events and errors are logged using the provided <see cref="ILogger"/>.
    /// </para>
    /// </summary>
    public class NetLibNetworkingServer : INetworkingServer
    {
        private readonly NetManager _netManager;
        private readonly IMessageSender _messageSender;
        private readonly EventBasedNetListener _eventListener;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly EntityRegistry _entityRegistry;
        private readonly IComponentSerializer _componentSerializer;
        private IDisposable? _pollHandle;
        private CancellationTokenSource? _cts;
        private volatile bool _running;
        private string _netSecret = "";

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingServer"/>.
        /// </summary>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking. Must be constructed with an EventBasedNetListener.</param>
        /// <param name="messageSender">The injected message sender for sending messages to clients.</param>
        /// <param name="eventListener">The injected eventBasedNetListener</param>
        /// <param name="logger">Logger for structured logging of network events.</param>
        /// <param name="scheduler">Scheduler for polling events.</param>
        /// <exception cref="ArgumentException">Thrown if the NetManager does not use an EventBasedNetListener.</exception>
        public NetLibNetworkingServer(NetManager netManager,
            IMessageSender messageSender,
            EventBasedNetListener eventListener,
            ILogger logger,
            IScheduler scheduler,
            EntityRegistry entityRegistry,
            IComponentSerializer componentSerializer)
        {
            _netManager = netManager;
            _messageSender = messageSender;
            _eventListener = eventListener;
            _logger = logger;
            _scheduler = scheduler;
            _entityRegistry = entityRegistry;
            _componentSerializer = componentSerializer;
        }

        /// <inheritdoc />
        public IDisposable StartServer(string address, int port, string netSecret = "")
        {
            if (_running)
            {
                throw new InvalidOperationException("Server is already running.");
            }

            _running = true;
            _netSecret = netSecret;
            _eventListener.ConnectionRequestEvent += OnConnectionRequest;
            _eventListener.PeerConnectedEvent += OnPeerConnected;

            // Use the address parameter if provided, otherwise fallback to default
            if (!string.IsNullOrWhiteSpace(address) &&
                address != "0.0.0.0" &&
                address != "localhost")
            {
                _netManager.Start(IPAddress.Parse(address), IPAddress.IPv6Any, port);
            }
            else
            {
                _netManager.Start(port);
            }

            _logger.Info(LoggedFeature.Networking, "Server started on {0}:{1}...", address, port);

            _cts = new CancellationTokenSource();
            _pollHandle = _scheduler.ScheduleAtFixedRate(
                () => _netManager.PollEvents(),
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(15),
                _cts.Token);

            return new ServerHandle(this);
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            var peer = _netSecret == "" ? request.Accept() : request.AcceptIfKey(_netSecret);
            if (peer == null)
            {
                _logger.Warn(LoggedFeature.Networking, "Connection request from {0} rejected.", request.RemoteEndPoint);
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            // Send ConnectedMessage to the client
            var msg = new ConnectedMessage
            {
                PeerId = peer.Id,
                ConnectionTime = DateTime.UtcNow,
                InitialWorldSnapshot = new WorldDeltaMessage(_componentSerializer)
                {
                    Deltas = _entityRegistry.GetAll().Select(e => new EntityDelta
                    {
                        EntityId = e.Id.Value,
                        IsNew = true,
                        AddedOrModifiedComponents = e.GetAllComponents().ToList()
                    }).ToList()
                }
            };

            _messageSender.SendMessage(peer.Id, MessageType.Connected, msg, ChannelType.ReliableOrdered);
            _logger.Info(LoggedFeature.Networking, "Sent ConnectedMessage to peer {0}", peer.Id);
        }

        private void Stop()
        {
            _running = false;
            _cts?.Cancel();
            _pollHandle?.Dispose();
            _netManager.Stop();

            // Unsubscribe event handlers to prevent memory leaks
            _eventListener.ConnectionRequestEvent -= OnConnectionRequest;
            _eventListener.PeerConnectedEvent -= OnPeerConnected;

            _logger.Info("Server stopped.");
        }

        /// <summary>
        /// Disposable handle for stopping the server.
        /// </summary>
        private sealed class ServerHandle : IDisposable
        {
            private readonly NetLibNetworkingServer _server;
            private bool _disposed;

            public ServerHandle(NetLibNetworkingServer server)
            {
                _server = server;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _server.Stop();
                    _disposed = true;
                }
            }
        }
    }
}