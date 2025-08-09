using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using Shared.ECS.Replication;
using Shared.Logging;
using Shared.Networking.Messages;
using Shared.Scheduling;
using ILogger = Shared.Logging.ILogger;

namespace Shared.Networking
{
    /// <summary>
    /// Simple LiteNetLib-based networking client.
    /// </summary>
    public class NetLibNetworkingClient : INetworkingClient, IDisposable
    {
        private readonly MessageFactory _messageFactory;
        private readonly ILogger _logger;
        private readonly NetManager _netManager;
        private readonly EventBasedNetListener _listener;
        private readonly IScheduler _scheduler;
        private CancellationTokenSource? _cts;
        private IDisposable? _pollHandle;

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingClient"/>.
        /// </summary>
        /// <param name="messageFactory">Factory for creating message instances.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking.</param>
        /// <param name="listener">The injected event-based listener for handling network events.</param>
        /// <param name="scheduler">Scheduler for polling events.</param>
        public NetLibNetworkingClient(
            MessageFactory messageFactory,
            ILogger logger,
            NetManager netManager,
            EventBasedNetListener listener,
            IScheduler scheduler)
        {
            _messageFactory = messageFactory;
            _logger = logger;
            _netManager = netManager;
            _listener = listener;
            _scheduler = scheduler;
        }

        /// <summary>
        /// Initiates an asynchronous connection to the server. This method is thread-safe.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="port">The server port.</param>
        /// <param name="netSecret">The connection secret.</param>
        /// <param name="timeoutSeconds">Timeout in seconds for the connection attempt.</param>
        /// <returns>
        /// A Task that completes with an <see cref="IDisposable"/> connection handle, or throws on failure.
        /// </returns>
        public async Task<IClientConnection> ConnectAsync(string address, int port, string netSecret = "",
            int timeoutSeconds = 10)
        {
            _cts = new CancellationTokenSource();
            _netManager.Start();
            _pollHandle = _scheduler.ScheduleAtFixedRate(
                () => _netManager.PollEvents(),
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(15),
                _cts.Token);

            // Set up a message receiver to handle the connection response
            // This has to happen before we connect to the server
            // and we must initialize it before connecting so that it can handle messages immediately.
            var messageReceiver = new NetLibBinaryMessageReceiver(_listener, _messageFactory, _logger);
            messageReceiver.Initialize();

            // Register a message handler for the ConnectedMessage
            NetPeer? connectedPeer = null;
            var handlerId = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<IClientConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var handlerRegistration = messageReceiver.RegisterMessageHandler<ConnectedMessage>(handlerId, (peerId, msg) =>
            {
                // Find the NetPeer by peerId
                var peer = _netManager.GetPeerById(peerId);
                if (peer == null)
                {
                    _logger.Error(LoggedFeature.Networking, $"Failed to connect: Peer with ID {peerId} not found.");
                    tcs.TrySetException(new Exception($"Peer with ID {peerId} not found."));
                    return;
                }

                if (msg.InitialWorldSnapshot == null)
                {
                    _logger.Error(LoggedFeature.Networking, $"Failed to connect: InitialWorldSnapshot is null.");
                    tcs.TrySetException(new Exception("Initial world snapshot is null."));
                    return;
                }

                connectedPeer = peer;
                var messageSender = new NetLibBinaryMessageSender(_netManager, _logger);
                var connection = new ClientConnection(peer,
                    _logger,
                    messageSender,
                    messageReceiver,
                    msg.InitialWorldSnapshot,
                    msg.PeerId);

                _logger.Debug(LoggedFeature.Networking, $"Client {peer.Id} connected. Address: {peer.Address}");
                tcs.TrySetResult(connection);
            });

            void OnDisconnected(NetPeer peer, DisconnectInfo info)
            {
                if (connectedPeer == null || Equals(peer, connectedPeer))
                {
                    tcs.TrySetException(new Exception($"Failed to connect: {info.Reason}"));
                }
            }

            _listener.PeerDisconnectedEvent += OnDisconnected;

            _logger.Info($"NetLibNetworkingClient: Connecting to server at {address}:{port}");
            _netManager.Connect(address, port, netSecret);

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, timeoutCts.Token)).ConfigureAwait(false);

                if (completedTask == tcs.Task)
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                else
                {
                    throw new TimeoutException("Connection attempt timed out.");
                }
            }
            finally
            {
                _listener.PeerDisconnectedEvent -= OnDisconnected;
            }
        }

        /// <summary>
        /// Disposes the client, stopping the polling loop and the NetManager.
        /// </summary>
        public void Dispose()
        {
            _logger.Debug("Disposing NetLibNetworkingClient...");

            _cts?.Cancel();
            _pollHandle?.Dispose();
            _netManager.Stop();
            _cts?.Dispose();
        }

        /// <summary>
        /// Represents a disposable handle to a single network connection.
        /// Disposing this object will disconnect the specific peer.
        /// </summary>
        private sealed class ClientConnection : IClientConnection
        {
            private readonly NetPeer _peer;
            private bool _disposed;
            private readonly ILogger logger;

            public int AssignedPeerId { get; }
            public IMessageSender MessageSender { get; }
            public IMessageReceiver MessageReceiver => _binaryMessageReceiver;
            private readonly NetLibBinaryMessageReceiver _binaryMessageReceiver;

            public int PingMs => _peer.Ping;

            public WorldDeltaMessage InitialWorldSnapshot { get; set; }

            public ClientConnection(NetPeer peer,
                ILogger logger,
                IMessageSender messageSender,
                NetLibBinaryMessageReceiver messageReceiver,
                WorldDeltaMessage initialWorldSnapshot,
                int assignedPeerId)
            {
                _peer = peer;
                this.logger = logger;
                MessageSender = messageSender;
                AssignedPeerId = assignedPeerId;
                InitialWorldSnapshot = initialWorldSnapshot;

                // We store the concrete implementation as we need
                // to manage its disposal
                _binaryMessageReceiver = messageReceiver;
            }

            /// <summary>
            /// Disconnects the peer from the server.
            /// </summary>
            public void Dispose()
            {
                if (_disposed || _peer.ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                logger.Info("Disconnecting from server...");
                _peer.Disconnect();
                _binaryMessageReceiver.Dispose();
                _disposed = true;
            }
        }
    }
}