using System;
using System.Linq;
using Adapters.ECS.Debugging;
using Adapters.Health;
using Core.ECS;
using Core.ECS.Prediction;
using Core.ECS.Simulation;
using Core.Input;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.ECS.Systems;
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
        public IServiceProvider ServiceProvider => _serviceProvider;
        
        public GameSceneServiceProvider(IServiceCollection serviceCollection, IClientConnection clientConnection)
        {
            // Register the core ECS services
            // This includes the replication system, tick sync, entity view system, etc.
            serviceCollection.RegisterEcsServices();
            
            // Input System. Before other systems can run, we need to ensure
            // that the input is available for that tick.
            serviceCollection.AddSingleton<InputSystem>();
            serviceCollection.AddSingleton<ISystem>(sp => sp.GetRequiredService<InputSystem>());
            serviceCollection.AddSingleton<IInputListener>(sp => sp.GetRequiredService<InputSystem>());
            
            // Prediction systems
            // Some systems (like VelocityPredictionSystem vs VelocitySystem) are used instead of their
            // regular counterparts to handle client-side prediction.
            serviceCollection.AddSingleton<ISystem, PredictedPlayerMovementSystem>();
            serviceCollection.AddSingleton<ISystem, VelocityPredictionSystem>();
            serviceCollection.AddSingleton<ISystem, FaceMoveDirectionSystem>();
            serviceCollection.AddSingleton<PredictedPlayerShotSystem>();
            serviceCollection.AddSingleton<ISystem>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            serviceCollection.AddSingleton<IDisposable>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            
            // Health / Damage systems
            serviceCollection.AddSingleton<ISystem, HealthBarRenderSystem>();

            // Entity lifecycle systems
            serviceCollection.AddSingleton<ISystem, SelfDestroyingSystem>();
            
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