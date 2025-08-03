using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
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
        private readonly ILogger _logger;
        private readonly NetManager _netManager;
        private readonly IMessageReceiver _messageReceiver;
        private readonly IScheduler _scheduler;
        private CancellationTokenSource? _cts;
        private IDisposable? _pollHandle;

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking.</param>
        /// <param name="messageReceiver">The message receiver for handling incoming messages.</param>
        /// <param name="scheduler">Scheduler for polling events.</param>
        public NetLibNetworkingClient(ILogger logger, NetManager netManager, IMessageReceiver messageReceiver, IScheduler scheduler)
        {
            _logger = logger;
            _netManager = netManager;
            _messageReceiver = messageReceiver;
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

            var tcs = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionAttempt = _netManager.Connect(address, port, netSecret);

            void OnConnected(ConnectedMessage msg)
            {
                _logger.Info($"Connected to server with ClientId: {msg.AssignedPeerId}");
                tcs.TrySetResult(new ClientConnection(connectionAttempt, _logger));
            }

            var subscriber =
                _messageReceiver.RegisterMessageHandler<ConnectedMessage>("NetLibNetworkingClient.OnConnected", OnConnected);

            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                subscriber.Dispose();
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

            public int AssignedPeerId
            {
                get { return _peer.Id; }
            }

            public IMessageSender MessageSender { get; }
            public IMessageReceiver MessageReceiver { get; }

            public ClientConnection(NetPeer peer,
                ILogger logger,
                IMessageSender messageSender,
                IMessageReceiver messageReceiver)
            {
                _peer = peer;
                this.logger = logger;
                MessageSender = messageSender;
                MessageReceiver = messageReceiver;
            }

            /// <summary>
            /// Disconnects the peer from the server.
            /// </summary>
            public void Dispose()
            {
                if (_disposed || _peer.ConnectionState != ConnectionState.Connected)
                    return;

                logger.Info("Disconnecting from server...");
                _peer.Disconnect();
                _disposed = true;
            }
        }
    }
}