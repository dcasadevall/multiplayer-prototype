using System;
using System.Linq;
using Adapters.Character;
using Adapters.Input;
using Core.ECS;
using Core.ECS.Simulation;
using Core.Input;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.Networking;
using Shared.Scheduling;

namespace Adapters
{
    /// <summary>
    /// Ths class is responsible for creating the service collection and
    /// initializing the service provider including core systems.
    /// It is our "root of composition".
    ///
    /// Additionally, it connects to the server at startup.
    /// </summary>
    public class GameSceneServiceProvider : IDisposable, IInitializable
    {
        private IServiceProvider _serviceProvider;
        
        public GameSceneServiceProvider(IServiceCollection serviceCollection, IClientConnection clientConnection)
        {
            // Game systems
            serviceCollection.AddSingleton<ISystem, PlayerViewSystem>();
            
            // Input
            serviceCollection.AddSingleton<InputListener>();
            serviceCollection.AddSingleton<IInputListener>(sp => sp.GetRequiredService<InputListener>());
            serviceCollection.AddSingleton<ITickable>(sp => sp.GetRequiredService<InputListener>());
            
            // World manager inits the ECS world. Make sure ECS services are registered too.
            serviceCollection.RegisterEcsServices();
            serviceCollection.AddSingleton<ClientWorldManager>();
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<ClientWorldManager>());
            serviceCollection.AddSingleton<IDisposable>(sp => sp.GetRequiredService<ClientWorldManager>());
            
            // Register the client connection and the message sender / receiver
            serviceCollection.AddSingleton(clientConnection);
            serviceCollection.AddSingleton(sp => sp.GetRequiredService<IClientConnection>().MessageSender);
            serviceCollection.AddSingleton(sp => sp.GetRequiredService<IClientConnection>().MessageReceiver);
            
            // Build the game scene service provider
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        public void Initialize()
        {
            // Initialize all services that implement IInitializable
            var initializables = _serviceProvider.GetServices<IInitializable>();
            initializables.ToList().ForEach(x => x.Initialize());
        }

        public void Dispose()
        {
            // Dispose all services that implement IDisposable
            var disposables = _serviceProvider.GetServices<IDisposable>();
            disposables.ToList().ForEach(x => x.Dispose());
            _serviceProvider = null;
        }
    }
}