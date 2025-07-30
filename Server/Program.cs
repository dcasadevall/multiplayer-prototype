using Server.Scenes;
using Shared.ECS.Simulation;

var world = new WorldBuilder()
    // .AddSystem(new MovementSystem())
    // .AddSystem(new HealthSystem())
    .Build();

SceneLoader.Load("Server/Scenes/basic_scene.json", world.EntityManager);
world.Start();

Console.ReadLine();
world.Dispose();