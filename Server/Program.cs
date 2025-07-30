using Server.Scenes;
using Shared.Clock;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.Systems;
using Shared.Scheduling;

var entityRegistry = new EntityRegistry();
var scheduler = new TimerScheduler();
SceneLoader.Load("Server/Scenes/basic_scene.json", entityRegistry);

// Create a fixed timestep world running at 30Hz
var world = new WorldBuilder(entityRegistry, scheduler)
    .WithFrequency(30) // 30Hz = 33.33ms per tick
    .AddSystem(new WorldDiagnosticsSystem())
    .AddSystem(new MovementSystem())
    .AddSystem(new HealthSystem())
    .Build();

Console.WriteLine("Starting fixed timestep world at 30Hz...");
world.Start();

Console.WriteLine("Press Enter to stop...");
Console.ReadLine();
world.Dispose();