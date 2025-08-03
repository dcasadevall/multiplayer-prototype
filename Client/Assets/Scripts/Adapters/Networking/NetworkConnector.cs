using System;
using Shared;
using Shared.Networking;
using Shared.Scheduling;
using ILogger = Shared.Logging.ILogger;

namespace Adapters.Networking
{
    /// <summary>
    /// Handles connecting to the game server using an <see cref="INetworkingClient"/>.
    /// 
    /// This class implements <see cref="IInitializable"/> so it can be automatically initialized at application start.
    /// It manages the network connection's lifetime and ensures proper disposal.
    /// </summary>
    public class NetworkConnector : IInitializable, IDisposable
    {
        private readonly INetworkingClient _client;
        private readonly ILogger _logger;
        private IDisposable _connection;

        /// <summary>
        /// Constructs a new <see cref="NetworkConnector"/>.
        /// </summary>
        /// <param name="client">The networking client used to connect to the server.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public NetworkConnector(INetworkingClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the network connection to the server.
        /// This is called automatically if registered as an <see cref="IInitializable"/>.
        /// </summary>
        public async void Initialize()
        {
            _logger.Debug($"Starting {nameof(GameServiceProvider)}");
            _connection = await _client.ConnectAsync(SharedConstants.ServerAddress, SharedConstants.ServerPort, SharedConstants.NetSecret);
            _logger.Debug($"Connected to {SharedConstants.ServerAddress}:{SharedConstants.ServerPort}");
        }

        /// <summary>
        /// Disposes the network connection and releases resources.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}