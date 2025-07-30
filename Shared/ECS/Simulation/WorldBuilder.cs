namespace Shared.ECS.Simulation;

/// <summary>
/// Builder for constructing a World with a set of systems.
/// </summary>
public class WorldBuilder
{
    private readonly List<ISystem> _systems = new();

    /// <summary>
    /// Add a system to the world being built.
    /// </summary>
    public WorldBuilder AddSystem(ISystem system)
    {
        _systems.Add(system);
        return this;
    }

    /// <summary>
    /// Build and return a new World instance with the configured systems.
    /// </summary>
    public World Build()
    {
        var world = new World(_systems);
        return world;
    }
}