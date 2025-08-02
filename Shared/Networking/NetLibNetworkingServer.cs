using System;
using System.Net;
using System.Threading;
using LiteNetLib;
using Shared.Logging;

namespace Shared.Networking
{
    /// <summary>
    /// An implementation of <see cref="INetworkingServer"/> using LiteNetLib for networking.
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
        private readonly EventBasedNetListener _eventListener;
        private readonly ILogger _logger;
        private Thread? _serverThread;
        private volatile bool _running;
        private string _netSecret = "";

        /// <summary>
        /// Constructs a new <see cref="NetLibNetworkingServer"/>.
        /// </summary>
        /// <param name="netManager">The LiteNetLib NetManager instance to use for networking. Must be constructed with an EventBasedNetListener.</param>
        /// <param name="eventListener">The injected eventBasedNetListener</param>
        /// <param name="logger">Logger for structured logging of network events.</param>
        /// <exception cref="ArgumentException">Thrown if the NetManager does not use an EventBasedNetListener.</exception>
        public NetLibNetworkingServer(NetManager netManager, EventBasedNetListener eventListener, ILogger logger)
        {
            _netManager = netManager;
            _eventListener = eventListener;
            _logger = logger;
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
            _eventListener.NetworkReceiveEvent += OnNetworkReceive;

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

            _logger.Info("Server started on {0}:{1}...", address, port);

            _serverThread = new Thread(ServerLoop) { IsBackground = true };
            _serverThread.Start();

            return new ServerHandle(this);
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            if (_netSecret == "")
            {
                request.Accept();
            }
            else
            {
                request.AcceptIfKey(_netSecret);
            }

            _logger.Info("Accepted connection request from {0}", request.RemoteEndPoint);
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            var message = reader.GetString();
            _logger.Info("Received message from {0}: {1}", peer.Address, message);
            reader.Recycle();
        }

        private void ServerLoop()
        {
            try
            {
                while (_running)
                {
                    _netManager.PollEvents();
                    Thread.Sleep(15);
                }
            }
            finally
            {
                _netManager.Stop();
                _logger.Info("Server stopped.");
            }
        }

        private void Stop()
        {
            _running = false;
            if (_serverThread != null && _serverThread.IsAlive)
            {
                _serverThread.Join();
            }

            // Unsubscribe event handlers to prevent memory leaks
            _eventListener.ConnectionRequestEvent -= OnConnectionRequest;
            _eventListener.NetworkReceiveEvent -= OnNetworkReceive;
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