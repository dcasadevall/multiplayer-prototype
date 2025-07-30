using Server.Scenes;
using Shared.ECS;
using Shared.ECS.Simulation;

var entityRegistry = new EntityRegistry();
var clock = new SystemClock();
var world = new WorldBuilder()
    // .AddSystem(new MovementSystem())
    // .AddSystem(new HealthSystem())
    .Build(clock, entityRegistry);

SceneLoader.Load("Server/Scenes/basic_scene.json", entityRegistry);
world.Start();

Console.ReadLine();
world.Dispose();