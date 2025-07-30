using Server.Scenes;
using Shared.Clock;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.Systems;

var entityRegistry = new EntityRegistry();
var clock = new SystemClock();

// Create a fixed timestep world running at 30Hz
var world = new WorldBuilder(clock, entityRegistry)
    .WithFrequency(30) // 30Hz = 33.33ms per tick
    .AddSystem(new MovementSystem())
    .AddSystem(new HealthSystem())
    .Build();

// Set up first tick event to load the scene
world.OnFirstTick += () =>
{
    Console.WriteLine("First tick - loading scene...");
    SceneLoader.Load("Server/Scenes/basic_scene.json", entityRegistry);
};

// Set up tick event for monitoring
world.OnTick += (tickIndex) =>
{
    if (tickIndex % 300 == 0) // Log every 10 seconds at 30Hz
    {
        Console.WriteLine($"Tick {tickIndex} - Entities: {entityRegistry.GetAll().Count()}");
    }
};

Console.WriteLine("Starting fixed timestep world at 30Hz...");
world.Start();

Console.WriteLine("Press Enter to stop...");
Console.ReadLine();
world.Dispose();