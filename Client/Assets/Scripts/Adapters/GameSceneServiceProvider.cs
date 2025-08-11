using System;
using System.Linq;
using Adapters.Health;
using Adapters.Rendering;
using Core.ECS;
using Core.ECS.Simulation;
using Core.Input;
using Core.Physics;
using Microsoft.Extensions.DependencyInjection;
using Shared.Damage;
using Shared.ECS;
using Shared.Networking;
using Shared.Physics;
using Shared.Respawn;
using Shared.Scheduling;
using Shared.Settings;

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
            // Register the settings instances from the client connection
            serviceCollection.AddSingleton(clientConnection.Settings.PlayerSettings);
            serviceCollection.AddSingleton(clientConnection.Settings.ProjectileSettings);
            serviceCollection.AddSingleton(clientConnection.Settings.BotSettings);
            serviceCollection.AddSingleton(clientConnection.Settings.SimulationSettings);
            serviceCollection.AddSingleton<SettingsValidator>();
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<SettingsValidator>());

            // Register the core ECS services
            // This includes the replication system, tick sync, entity view system, etc.
            serviceCollection.RegisterEcsServices();
            
            // Input System. Before other systems can run, we need to ensure
            // that the input is available for that tick.
            serviceCollection.AddSingleton<InputSystem>();
            serviceCollection.AddSingleton<ISystem>(sp => sp.GetRequiredService<InputSystem>());
            serviceCollection.AddSingleton<IInputListener>(sp => sp.GetRequiredService<InputSystem>());
            
            // ----- Prediction systems -----
            // Some systems (like VelocityPredictionSystem vs VelocitySystem) are used instead of their
            // regular counterparts to handle client-side prediction.
            
            // Prediction: Player and other entities movement
            serviceCollection.AddSingleton<ISystem, PredictedPlayerMovementSystem>();
            serviceCollection.AddSingleton<ISystem, VelocityPredictionSystem>();
            
            // Prediction: Player shots
            serviceCollection.AddSingleton<PredictedPlayerShotSystem>();
            serviceCollection.AddSingleton<ISystem>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            serviceCollection.AddSingleton<IInitializable>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            serviceCollection.AddSingleton<IDisposable>(sp => sp.GetRequiredService<PredictedPlayerShotSystem>());
            
            // There are some common systems that are used for both client and server.
            // We could decide to put these inside the shared RegisterECSServices method.
            serviceCollection.AddSingleton<ISystem, WorldAABBUpdateSystem>();
            serviceCollection.AddSingleton<CollisionSystem>();
            serviceCollection.AddSingleton<ISystem>(sp => sp.GetRequiredService<CollisionSystem>());
            serviceCollection.AddSingleton<ICollisionDetector>(sp => sp.GetRequiredService<CollisionSystem>());
            serviceCollection.AddSingleton<ISystem, WorldAABBRenderSystem>();

            serviceCollection.AddSingleton<ISystem, UnitCollisionSystem>();
            
            // Health / Damage systems
            // Notice death system is not registered here, we want that to be fully server-side.
            serviceCollection.AddSingleton<ISystem, HealthBarRenderSystem>();
            serviceCollection.AddSingleton<ISystem, DamageSystem>();

            // Rendering stuff
            serviceCollection.AddSingleton<ISystem, ColorRenderingSystem>();

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