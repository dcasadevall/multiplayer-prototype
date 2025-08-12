using System;
using System.Net;
using System.Threading;
using LiteNetLib;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking.Messages;
using Shared.Scheduling;
using Shared.ECS.Entities;
using System.Linq;
using Shared.Replication;
using Shared.Settings;

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
        private readonly IWorldSnapshotProvider _worldSnapshotProvider;
        private readonly IComponentSerializer _componentSerializer;
        private readonly ComponentTypeRegistry _componentTypeRegistry;
        private readonly PlayerSettings _playerSettings;
        private readonly ProjectileSettings _projectileSettings;
        private readonly BotSettings _botSettings;
        private readonly SimulationSettings _simulationSettings;
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
        /// <param name="worldSnapshotProvider">Provider for world snapshots to send to clients.</param>
        /// <param name="componentSerializer"></param>
        /// <param name="componentTypeRegistry"></param>
        /// <exception cref="ArgumentException">Thrown if the NetManager does not use an EventBasedNetListener.</exception>
        public NetLibNetworkingServer(NetManager netManager,
            IMessageSender messageSender,
            EventBasedNetListener eventListener,
            ILogger logger,
            IScheduler scheduler,
            IWorldSnapshotProvider worldSnapshotProvider,
            IComponentSerializer componentSerializer,
            ComponentTypeRegistry componentTypeRegistry,
            PlayerSettings playerSettings,
            ProjectileSettings projectileSettings,
            BotSettings botSettings,
            SimulationSettings simulationSettings)
        {
            _netManager = netManager;
            _messageSender = messageSender;
            _eventListener = eventListener;
            _logger = logger;
            _scheduler = scheduler;
            _worldSnapshotProvider = worldSnapshotProvider;
            _componentSerializer = componentSerializer;
            _componentTypeRegistry = componentTypeRegistry;
            _playerSettings = playerSettings;
            _projectileSettings = projectileSettings;
            _botSettings = botSettings;
            _simulationSettings = simulationSettings;
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

            // Bind strategy:
            // - If a concrete address/hostname is provided (e.g., "fly-global-services"), resolve and bind to IPv4.
            // - Otherwise, bind on the default interface by port only.
            try
            {
                if (!string.IsNullOrWhiteSpace(address) && address != "0.0.0.0" && address != "localhost")
                {
                    // Try DNS resolution (supports hostnames like "fly-global-services")
                    var addresses = Dns.GetHostAddresses(address);
                    var ipv4 = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if (ipv4 != null)
                    {
                        _netManager.Start(ipv4, IPAddress.IPv6Any, port);
                        _logger.Info(LoggedFeature.Networking, "Server bound UDP on {0}:{1}", ipv4, port);
                    }
                    else
                    {
                        // Fallback to default bind
                        _netManager.Start(port);
                        _logger.Warn(LoggedFeature.Networking, "Could not resolve IPv4 for {0}. Bound on default interface for port {1}",
                            address, port);
                    }
                }
                else
                {
                    _netManager.Start(port);
                    _logger.Info(LoggedFeature.Networking, "Server bound UDP on default interface at port {0}", port);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(LoggedFeature.Networking,
                    "Failed to bind on {0}:{1} - {2}. Falling back to default.",
                    address,
                    port,
                    ex.Message);
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
                InitialWorldSnapshot = new WorldDeltaMessage(_componentSerializer, _componentTypeRegistry)
                {
                    Deltas = _worldSnapshotProvider.ProduceEntitySnapshot()
                },
                Settings = new SettingsMessage
                {
                    PlayerSettings = _playerSettings,
                    ProjectileSettings = _projectileSettings,
                    BotSettings = _botSettings,
                    SimulationSettings = _simulationSettings
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