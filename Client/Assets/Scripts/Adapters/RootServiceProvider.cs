using System;
using System.Linq;
using Core.Logging;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.Networking;
using Shared.Scheduling;
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
    public class RootServiceProvider : MonoBehaviour
    {
        [SerializeField]
        private string _gameSceneName = "GameScene";
        
        [SerializeField]
        private UnityMainThreadScheduler _mainThreadScheduler;

        [SerializeField] 
        private Transform _loginScreen;
        
        private IServiceProvider _serviceProvider;
        private IServiceCollection _services;
        private GameSceneServiceProvider _gameSceneServiceProvider;
        
        /// <summary>
        /// Allow access to the service provider for the login scene for other unity
        /// behaviors. This should be used for debugging purposes only.
        /// </summary>
        public IServiceProvider ServiceProvider => _gameSceneServiceProvider?.ServiceProvider;
        
        private void Awake()
        {
            // Initialize the message factory before any other services are configured
            MessageFactory.Initialize();
            ComponentSerializer.Initialize();
            
            // 1. Build a persistent, root service provider for networking
            _services = new ServiceCollection();

            // Logging
            _services.AddSingleton<ILogger, UnityLogger>();
            
            // Register the main thread scheduler
            _services.AddSingleton<IScheduler>(_mainThreadScheduler);
            
            // Networking, so we can connect to the server
            _services.RegisterNetLibTypes();
            
            // Tick scheduling (IInitializable, IDisposable are handled via this class lifeycle)
            _services.RegisterSchedulingTypes();

            // Build the service provider for LoginScene
            _serviceProvider = _services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Start is used to initialize all services that implement <see cref="IInitializable"/>.
        /// </summary>
        private async void Start()
        {
            // 1. Initialize all services that implement IInitializable
            var initializables = _serviceProvider.GetServices<IInitializable>();
            initializables.ToList().ForEach(x => x.Initialize()); 
            
            // 2. Connect to the server
            var client = _serviceProvider.GetRequiredService<INetworkingClient>();
            
            Debug.Log("Login: Connecting to server...");
            var connection = await client.ConnectAsync(SharedConstants.ServerAddress, 
                SharedConstants.ServerPort, 
                SharedConstants.NetSecret);
            
            Debug.Log($"Login: Connected successfully! Peer ID: {connection.AssignedPeerId}");

            // 3. Load the main game scene and hide UI
            SceneManager.LoadScene(_gameSceneName, LoadSceneMode.Additive);
            _loginScreen.gameObject.SetActive(false);
            
            // 4. Initialize the GameSceneServiceProvider
            _gameSceneServiceProvider = new GameSceneServiceProvider(_services, connection);
            _gameSceneServiceProvider.Initialize();
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
            
            Debug.Log("LoginSceneServiceProvider: Disposed successfully");
        }
    }
}