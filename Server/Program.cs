using System.Net;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Server.Logging;
using Server.Networking;
using Server.Networking.Replication;
using Server.PlayerSpawn;
using Server.Scenes;
using Shared;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.Systems;
using Shared.Logging;
using Shared.Scheduling;

var services = new ServiceCollection();

// Register ECS core
services.AddSingleton<EntityRegistry>();
    
// Server side logging
services.AddSingleton<ILogger, ConsoleLogger>();

// Register server systems
services.AddSingleton<ISystem, WorldDiagnosticsSystem>();
services.AddSingleton<ISystem, MovementSystem>();
services.AddSingleton<ISystem, HealthSystem>();
services.AddSingleton<ISystem, ReplicationSystem>();

// Scene loading
services.AddSingleton<SceneLoader>();

// Player related services
services.AddSingleton<PlayerSpawnHandler>();

// Register all shared services (Networking, Scheduling, etc.)
services.RegisterSharedTypes();

// Server proxies events with NetEventBroadcaster
services.AddSingleton<NetEventBroadcaster>();
services.AddSingleton<INetEventListener>(sp => sp.GetRequiredService<NetEventBroadcaster>());
services.AddSingleton<NetManager>();

var serviceProvider = services.BuildServiceProvider();
var entityRegistry = serviceProvider.GetRequiredService<EntityRegistry>();
var scheduler = serviceProvider.GetRequiredService<IScheduler>();
var sceneLoader = serviceProvider.GetRequiredService<SceneLoader>();
var netManager = serviceProvider.GetRequiredService<NetManager>();

// TODO: IInitializable / IDisposable and auto lifecycle management
var spawnHandler = serviceProvider.GetRequiredService<PlayerSpawnHandler>();

// TODO: More robust scene loading eventually
sceneLoader.Load("Server/Scenes/basic_scene.json");

// Create a fixed timestep world running at 30Hz
// Add all the systems registered in the service provider
var worldBuilder = new WorldBuilder(entityRegistry, scheduler).WithFrequency(30); 
var systems = serviceProvider.GetServices<ISystem>().ToList();
systems.ForEach(x => worldBuilder.AddSystem(x));
var world = worldBuilder.Build();

Console.WriteLine("Starting fixed timestep world at 30Hz...");
world.Start();

netManager.Start(9050);
Console.WriteLine("Server started on port 9050...");

// --- The Server Loop ---
try
{
    while (true)
    {
        // Poll for new events
        netManager.PollEvents();
        // Sleep to prevent high CPU usage
        Thread.Sleep(15);
    }
}
finally
{
    // Ensure the server is stopped when the loop exits
    netManager.Stop();
    world.Dispose();
    spawnHandler.Dispose();
}