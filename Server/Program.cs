using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Server.Logging;
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
services.AddSingleton<EventBasedNetListener>();
services.AddSingleton<INetEventListener>(sp => sp.GetRequiredService<EventBasedNetListener>());
services.AddSingleton<NetManager>();

var serviceProvider = services.BuildServiceProvider();
var entityRegistry = serviceProvider.GetRequiredService<EntityRegistry>();
var scheduler = serviceProvider.GetRequiredService<IScheduler>();
var sceneLoader = serviceProvider.GetRequiredService<SceneLoader>();
var netManager = serviceProvider.GetRequiredService<NetManager>();
var eventListener = serviceProvider.GetRequiredService<EventBasedNetListener>();

// Auto accept connection requests
// This is fine for a simple server, but in a real game you would want to handle this more robustly
eventListener.ConnectionRequestEvent += request =>
{
    request.AcceptIfKey(SharedConstants.NetSecret);
};

eventListener.NetworkReceiveEvent += (peer, reader, channel, method) =>
{
    // Log incoming messages. For now, convert the message to a string for simplicity.
    var message = reader.GetString();
    Console.WriteLine($"Received message from {peer.Address}: {message}");
};


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
world.Start();

netManager.Start(SharedConstants.ServerPort);
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