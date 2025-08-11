using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Logging;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Shared.Logging;
using Shared.Networking;
using Shared.Scheduling;
using Shared.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using ILogger = Shared.Logging.ILogger;

namespace Adapters
{
    /// <summary>
    /// Provides and configures the services required for the root scene.
    /// 
    /// This MonoBehaviour sets up a root service provider with logging, scheduling, and networking dependencies
    /// needed for the login process. It connects to the server, then loads the main game scene and initializes
    /// the <c>GameSceneServiceProvider</c>, which will provide all dependencies for the game scene.
    /// </summary>
    public class GameLauncher : MonoBehaviour
    {
        /// <summary>
        /// Event called when the root services are configured.
        /// This includes logging, scheduling, and networking services.
        /// No connection to the server is made at this point, but we can
        /// call <see cref="StartGameAsync"/> to connect to the server.
        /// </summary>
        public event Action<IServiceProvider> OnRootServicesConfigured;
        
        /// <summary>
        /// Event called when the game has started.
        /// Passes the service provider including all game services.
        /// At this point, the game scene is loaded and the <see cref="IClientConnection"/>
        /// is available for use.
        /// </summary>
        public event Action<IServiceProvider> OnGameStarted;
        
        [SerializeField]
        private string _gameSceneName = "GameScene";
        
        [SerializeField] 
        private Transform _loginScreen;

        [SerializeField] 
        private Settings.GameSettings _gameSettings;
        private NetworkSettings NetworkSettings => _gameSettings.NetworkSettings;
        
        private IServiceProvider _serviceProvider;
        private IServiceCollection _services;
        private GameSceneServiceProvider _gameSceneServiceProvider;
        private ILogger _logger;
        private IClientConnection _connection;
        
        /// <summary>
        /// Allow access to the service provider for the login scene for other unity
        /// behaviors. This should be used for debugging purposes only.
        /// </summary>
        public IServiceProvider ServiceProvider => _gameSceneServiceProvider?.ServiceProvider;
        
        private void Awake()
        {
            // 1. Build a persistent, root service provider for networking
            _services = new ServiceCollection();

            // Logging
            _services.AddSingleton<ILogger, UnityLogger>();
            
            // Register the main thread scheduler
            _services.AddSingleton<IScheduler, UnityMainThreadScheduler>();
            
            // Networking, so we can connect to the server
            _services.RegisterNetLibTypes();
            
            // Tick scheduling (IInitializable, IDisposable are handled via this class lifeycle)
            _services.RegisterSchedulingTypes();

            // Build the service provider for LoginScene
            _serviceProvider = _services.BuildServiceProvider();
            
            // Keep a reference to the logger for internal use
            _logger = _serviceProvider.GetRequiredService<ILogger>();
        }
        
        /// <summary>
        /// Start is used to initialize all services that implement <see cref="IInitializable"/>.
        /// </summary>
        private void Start()
        {
            // 1. Initialize all "root" services that implement IInitializable.
            var initializables = _serviceProvider.GetServices<IInitializable>();
            initializables.ToList().ForEach(x => x.Initialize());
            
            // 2. Signal that the root services are ready for the splash screen to use.
            _logger.Info("Root services configured.");
            OnRootServicesConfigured?.Invoke(_serviceProvider);
        }
        
        /// <summary>
        /// Connects to the server, loads the game scene, and initializes game-specific services.
        /// This method should be called by the UI (e.g., Splash Screen) after the user
        /// initiates the login/connect action.
        /// </summary>
        public async Task StartGameAsync()
        {
            // 1. Connect to the server
            var client = _serviceProvider.GetRequiredService<INetworkingClient>();
            
            _logger.Info(LoggedFeature.Networking, "Connecting to server...");
            _connection = await client.ConnectAsync(NetworkSettings.ServerAddress, 
                NetworkSettings.ServerPort, 
                NetworkSettings.NetSecret);
            
            _logger.Info(LoggedFeature.Networking, $"Connected successfully. Peer ID: {_connection.AssignedPeerId}");

            // 2. Load the main game scene
            await SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Additive);
            
            // 3. Initialize the GameSceneServiceProvider
            _gameSceneServiceProvider = new GameSceneServiceProvider(_services, _connection);
            _gameSceneServiceProvider.Initialize();
            
            // 4. Subscribe to disconnects so we can reload the login scene if disconnected
            _connection.OnDisconnected += HandleOnDisconnected;
            
            // 5. Notify that the game has started and the service provider is ready
            OnGameStarted?.Invoke(_gameSceneServiceProvider.ServiceProvider);
        }

        private void HandleOnDisconnected()
        {
            _connection.OnDisconnected -= HandleOnDisconnected;
            _logger.Warn(LoggedFeature.Networking, "Disconnected from server. Reloading login scene...");
            SceneManager.LoadSceneAsync("LoginScene", LoadSceneMode.Single);
        }
        
        /// <summary>
        /// Disposes of the service provider and cleans up resources.
        /// Calls Dispose on all registered IDisposable services.
        /// </summary>
        private void OnDestroy()
        {
            // Dispose all registered IDisposable services
            var disposables = _serviceProvider.GetServices<IDisposable>();
            disposables.ToList().ForEach(x => x.Dispose());
            _serviceProvider = null;
            
            // Dispose the GameSceneServiceProvider
            _gameSceneServiceProvider?.Dispose();
            _gameSceneServiceProvider = null;

            if (_connection != null)
            {
                _connection.OnDisconnected -= HandleOnDisconnected;
                _connection = null;
            }

            Debug.Log("LoginSceneServiceProvider: Disposed successfully");
        }
    }
}