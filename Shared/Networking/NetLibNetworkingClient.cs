using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using Shared.Logging;
using Shared.Scheduling;
using ILogger = Shared.Logging.ILogger;

namespace Shared.Networking
{
    /// <summary>
    /// Simple LiteNetLib-based networking client.
    /// </summary>
    public class NetLibNetworkingClient : INetworkingClient, IDisposable
    {
        private readonly ILogger _logger;
        private readonly NetManager _netManager;
        private readonly EventBasedNetListener _listener;
        private readonly IScheduler _scheduler;
        private CancellationTokenSource? _cts;
        private IDisposable? _pollHandle;

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking.</param>
        /// <param name="listener">The injected event-based listener for handling network events.</param>
        /// <param name="scheduler">Scheduler for polling events.</param>
        public NetLibNetworkingClient(ILogger logger, NetManager netManager, EventBasedNetListener listener, IScheduler scheduler)
        {
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

            var tcs = new TaskCompletionSource<IClientConnection>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionAttempt = _netManager.Connect(address, port, netSecret);

            void OnConnected(NetPeer peer)
            {
                if (Equals(peer.Address, connectionAttempt.Address))
                {
                    var messageSender = new NetLibJsonMessageSender(_netManager, _logger);
                    var messageReceiver = new NetLibJsonMessageReceiver(_listener, _logger);
                    var connection = new ClientConnection(peer, _logger, messageSender, messageReceiver);

                    _logger.Debug(LoggedFeature.Networking, $"Client {peer.Id} connected. Address: {peer.Address}");
                    tcs.TrySetResult(connection);
                }
            }

            void OnDisconnected(NetPeer peer, DisconnectInfo info)
            {
                if (Equals(peer.Address, connectionAttempt.Address))
                {
                    tcs.TrySetException(new Exception($"Failed to connect: {info.Reason}"));
                }
            }

            _listener.PeerConnectedEvent += OnConnected;
            _listener.PeerDisconnectedEvent += OnDisconnected;

            _logger.Info($"NetLibNetworkingClient: Connecting to server at {address}:{port}");

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
                _listener.PeerConnectedEvent -= OnConnected;
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

            public int AssignedPeerId => _peer.Id;
            public IMessageSender MessageSender { get; }
            public IMessageReceiver MessageReceiver => _jsonMessageReceiver;
            private readonly NetLibJsonMessageReceiver _jsonMessageReceiver;

            public ClientConnection(NetPeer peer,
                ILogger logger,
                IMessageSender messageSender,
                NetLibJsonMessageReceiver messageReceiver)
            {
                _peer = peer;
                this.logger = logger;
                MessageSender = messageSender;

                // We store the concrete implementation as we need
                // to manage its lifecycle as part of this connection.
                _jsonMessageReceiver = messageReceiver;
                _jsonMessageReceiver.Initialize();
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
                _jsonMessageReceiver.Dispose();
                _disposed = true;
            }
        }
    }
}