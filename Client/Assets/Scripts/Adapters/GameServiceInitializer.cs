using System;
using Adapters.Character;
using Adapters.Input;
using Core;
using Core.ECS.Simulation;
using Core.Input;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.ECS;
using Shared.Networking;
using Shared.Scheduling;
using UnityEngine;

namespace Adapters
{
    /// <summary>
    /// Ths class is responsible for creating the service collection and
    /// initializing the service provider including core systems.
    /// It is our "root of composition".
    ///
    /// Additionally, it connects to the server at startup.
    /// </summary>
    public class GameServiceInitializer : MonoBehaviour
    {
        [SerializeField] 
        private UnityServiceProvider serviceProvider;

        private IDisposable _connection;
        
        private void Awake()
        {
            var serviceCollection = new ServiceCollection();
            
            // Game systems
            serviceCollection.AddSingleton<ISystem, PlayerViewSystem>();
            
            // Input
            serviceCollection.AddSingleton<InputListener>();
            serviceCollection.AddSingleton<IInputListener>(sp => sp.GetRequiredService<InputListener>());
            serviceCollection.AddSingleton<ITickable>(sp => sp.GetRequiredService<InputListener>());
            
            // World
            serviceCollection.AddSingleton<ClientWorldManager>();
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<ClientWorldManager>());
            serviceCollection.AddSingleton<IDisposable>(sp => sp.GetRequiredService<ClientWorldManager>());
            
            // Core dependencies 
            serviceProvider.RegisterCoreServices(serviceCollection);
        }

        private async void Start()
        {
            var client = serviceProvider.GetService<INetworkingClient>();
            Debug.Log($"Starting {nameof(GameServiceInitializer)}");
            _connection = await client.ConnectAsync(SharedConstants.ServerAddress, SharedConstants.ServerPort, SharedConstants.NetSecret);
            Debug.Log($"Connected to {SharedConstants.ServerAddress}:{SharedConstants.ServerPort}");
        }
        
        private void OnDestroy()
        {
            // Dispose of the connection when the game object is destroyed
            _connection?.Dispose();
            _connection = null;
        }
    }
}