using Server.Scenes;
using Shared.ECS;
using Shared.ECS.Simulation;

var entityRegistry = new EntityRegistry();
var clock = new SystemClock();
var systems = new List<ISystem>
{
    // new MovementSystem(),
    // new HealthSystem()
};
var world = new World(systems, clock, entityRegistry);

SceneLoader.Load("Server/Scenes/basic_scene.json", entityRegistry);
world.Start();

Console.ReadLine();
world.Dispose();