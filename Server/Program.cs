using Microsoft.Extensions.DependencyInjection;
using Server.Logging;
using Server.PlayerSpawn;
using Server.Replication;
using Server.Scenes;
using Shared;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.Systems;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking;
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
services.AddSingleton<ISystem, ServerTickSystem>();
services.AddSingleton<ISystem, ReplicationSystem>();

// Scene loading
services.AddSingleton<SceneLoader>();

// Player related services
services.AddSingleton<PlayerSpawnHandler>();

// Register all shared services (Networking, Scheduling, etc.)
services.RegisterSharedTypes();

// Register message sender and receiver, as the server
// does not have a stateful connection object like the client.
services.AddSingleton<IMessageSender, NetLibJsonMessageSender>();
services.AddSingleton<NetLibJsonMessageReceiver>();
services.AddSingleton<IMessageReceiver>(sp => sp.GetRequiredService<NetLibJsonMessageReceiver>());

// The scheduler is server specific (client will use a different scheduler)
services.AddSingleton<IScheduler, TimerScheduler>();

// Register the networking server abstraction
services.AddSingleton<INetworkingServer, NetLibNetworkingServer>();

var serviceProvider = services.BuildServiceProvider();
var entityRegistry = serviceProvider.GetRequiredService<EntityRegistry>();
var scheduler = serviceProvider.GetRequiredService<IScheduler>();
var sceneLoader = serviceProvider.GetRequiredService<SceneLoader>();
var messageReceiver = serviceProvider.GetRequiredService<NetLibJsonMessageReceiver>();

// TODO: IInitializable / IDisposable and auto lifecycle management
var spawnHandler = serviceProvider.GetRequiredService<PlayerSpawnHandler>();

var path = Path.Combine(AppContext.BaseDirectory, "Scenes", "basic_scene.json");
sceneLoader.Load(path);

// Create a fixed timestep world running at the specified frequency
// Add all the systems registered in the service provider
var worldBuilder = new WorldBuilder(entityRegistry, scheduler).WithFrequency(SharedConstants.WorldTickRate);
var systems = serviceProvider.GetServices<ISystem>().ToList();
systems.ForEach(x => worldBuilder.AddSystem(x));
var world = worldBuilder.Build();

Console.WriteLine("Starting fixed timestep world at 30Hz...");
messageReceiver.Initialize();
world.Start();

// Start the networking server using the abstraction
var networkingServer = serviceProvider.GetRequiredService<INetworkingServer>();
var serverHandle = networkingServer.StartServer(SharedConstants.ServerAddress,
    SharedConstants.ServerPort,
    SharedConstants.NetSecret);

try
{
    // Wait for exit (could be a signal, keypress, etc.)
    Console.WriteLine("Press Ctrl+C to exit...");
    Thread.Sleep(Timeout.Infinite);
}
finally
{
    // Ensure the server is stopped when the loop exits
    serverHandle.Dispose();
    world.Dispose();
    spawnHandler.Dispose();
    messageReceiver.Dispose();
}