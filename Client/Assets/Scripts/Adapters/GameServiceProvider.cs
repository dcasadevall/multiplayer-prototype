using System;
using Adapters.Character;
using Adapters.Input;
using Adapters.Networking;
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
    public class GameServiceProvider : MonoBehaviour
    {
        [SerializeField] 
        private CoreServiceProvider serviceProvider;

        private IDisposable _connection;
        private IServiceProvider _provider;
        
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
            
            // Networking
            serviceCollection.AddSingleton<NetworkConnector>();
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<NetworkConnector>());
            serviceCollection.AddSingleton<IDisposable>(sp => sp.GetRequiredService<NetworkConnector>());
            
            // Core dependencies 
            _provider = serviceProvider.RegisterCoreServices(serviceCollection);
        }
        
        private void OnDestroy()
        {
            // Dispose of the connection when the game object is destroyed
            _connection?.Dispose();
            _connection = null;
        }
    }
}