using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using ILogger = Shared.Logging.ILogger;

namespace Shared.Networking
{
    /// <summary>
    /// An implementation of <see cref="INetworkingClient"/> using LiteNetLib for networking.
    /// This client is reusable for multiple connection attempts. Each successful connection
    /// returns a disposable handle to manage the connection's lifecycle.
    /// </summary>
    public class NetLibNetworkingClient : INetworkingClient, IDisposable
    {
        private readonly ILogger _logger;
        private readonly NetManager _netManager;
        private readonly EventBasedNetListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _pollTask;

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingClient"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking.</param>
        /// <param name="listener">The injected EventBasedNetListener for handling network events.</param>
        public NetLibNetworkingClient(ILogger logger, NetManager netManager, EventBasedNetListener listener)
        {
            _logger = logger;
            _netManager = netManager;
            _listener = listener;
            _netManager.Start();
            _pollTask = Task.Run(PollLoop, _cts.Token);
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
        public async Task<IDisposable> ConnectAsync(string address, int port, string netSecret = "",
            int timeoutSeconds = 10)
        {
            var tcs = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionAttempt = _netManager.Connect(address, port, netSecret);

            void OnConnected(NetPeer peer)
            {
                // On success, wrap the peer in our disposable handle.
                if (Equals(peer, connectionAttempt))
                {
                    tcs.TrySetResult(new ClientConnection(peer));
                }
            }

            void OnDisconnected(NetPeer peer, DisconnectInfo info)
            {
                if (Equals(peer, connectionAttempt))
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
                await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _listener.PeerConnectedEvent -= OnConnected;
                _listener.PeerDisconnectedEvent -= OnDisconnected;
            }
        }

        private async Task PollLoop()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    _netManager.PollEvents();
                    await Task.Delay(15, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Dispose is called.
            }
        }

        /// <summary>
        /// Disposes the client, stopping the polling loop and the NetManager.
        /// </summary>
        public void Dispose()
        {
            if (_cts.IsCancellationRequested) return;

            _cts.Cancel();
            _pollTask.Wait(); // Note: This blocks until the polling task finishes.
            _netManager.Stop();
            _cts.Dispose();
        }

        /// <summary>
        /// Represents a disposable handle to a single network connection.
        /// Disposing this object will disconnect the specific peer.
        /// </summary>
        private sealed class ClientConnection : IDisposable
        {
            /// <summary>
            /// The underlying LiteNetLib network peer for this connection.
            /// </summary>
            private NetPeer Peer { get; }

            private bool _disposed;

            public ClientConnection(NetPeer peer)
            {
                Peer = peer;
            }

            /// <summary>
            /// Disconnects the peer from the server.
            /// </summary>
            public void Dispose()
            {
                if (_disposed || Peer.ConnectionState != ConnectionState.Connected)
                {
                    return;
                }

                Peer.Disconnect();
                _disposed = true;
            }
        }
    }
}