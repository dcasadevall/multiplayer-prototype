using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Scenes;
using Shared;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.ECS.TickSync;
using Shared.Networking;
using Shared.Scheduling;
using Shared.Settings;

namespace Server
{
    public class Main : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IServiceProvider _serviceProvider;
        private IDisposable? _serverHandle;
        private World? _world;

        public Main(IHostApplicationLifetime appLifetime, IServiceProvider serviceProvider)
        {
            _appLifetime = appLifetime;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        var entityRegistry = _serviceProvider.GetRequiredService<EntityRegistry>();
                        var scheduler = _serviceProvider.GetRequiredService<IScheduler>();
                        var sceneLoader = _serviceProvider.GetRequiredService<SceneLoader>();
                        var tickSync = _serviceProvider.GetRequiredService<ITickSync>();
                        var networkSettings = _serviceProvider.GetRequiredService<NetworkSettings>();

                        // Handle heroku port. required for heroku deployment.
                        var envPort = Environment.GetEnvironmentVariable("PORT");
                        if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out var herokuPort))
                        {
                            // Align UDP server port with Heroku assigned port.
                            networkSettings.ServerPort = herokuPort;
                        }

                        var simulationSettings = _serviceProvider.GetRequiredService<SimulationSettings>();

                        // Initialize all initializable services
                        foreach (var initializable in _serviceProvider.GetServices<IInitializable>())
                        {
                            initializable.Initialize();
                        }

                        // Scene / World loading
                        var path = Path.Combine(AppContext.BaseDirectory, "Scenes", "basic_scene.json");
                        sceneLoader.Load(path);

                        // Create a fixed timestep world
                        var worldBuilder = new WorldBuilder(entityRegistry, tickSync, scheduler)
                            .WithFrequency(simulationSettings.WorldTicksPerSecond)
                            .WithWorldMode(WorldMode.Server);

                        var systems = _serviceProvider.GetServices<ISystem>().ToList();
                        systems.ForEach(x => worldBuilder.AddSystem(x));
                        _world = worldBuilder.Build();
                        _world.Start();

                        Console.WriteLine($"Starting fixed timestep world at {simulationSettings.WorldTicksPerSecond}Hz...");

                        // Start the networking server
                        var networkingServer = _serviceProvider.GetRequiredService<INetworkingServer>();
                        _serverHandle = networkingServer.StartServer(networkSettings.ServerAddress, networkSettings.ServerPort,
                            networkSettings.NetSecret);

                        Console.WriteLine("Press Ctrl+C to exit...");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"An error occurred during startup: {ex.Message}");
                        _appLifetime.StopApplication();
                    }
                }, cancellationToken);
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Ensure the server is stopped when the application exits
            _serverHandle?.Dispose();
            _world?.Dispose();

            // Dispose all services that implement IDisposable
            foreach (var disposable in _serviceProvider.GetServices<IDisposable>())
            {
                disposable.Dispose();
            }

            return Task.CompletedTask;
        }
    }
}