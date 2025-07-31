using Microsoft.Extensions.DependencyInjection;
using Server.Logging;
using Server.Networking.Replication;
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

// Register all shared services (Networking, Scheduling, etc.)
services.RegisterSharedTypes();

var serviceProvider = services.BuildServiceProvider();
var entityRegistry = serviceProvider.GetRequiredService<EntityRegistry>();
var scheduler = serviceProvider.GetRequiredService<IScheduler>();
var sceneLoader = serviceProvider.GetRequiredService<SceneLoader>();

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

Console.WriteLine("Press Enter to stop...");
Console.ReadLine();
world.Dispose();