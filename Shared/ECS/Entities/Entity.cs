namespace Shared.ECS;

/// <summary>
/// Represents a single entity in the ECS world.
/// Entities are identified by an <see cref="EntityId"/> and are composed of <see cref="IComponent"/>s.
/// </summary>
public class Entity(EntityId id)
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public EntityId Id { get; } = id;

    /// <summary>
    /// Add a component to the entity.
    /// </summary>
    /// <typeparam name="T">The type of the component to add.</typeparam>
    /// <param name="component">The component to add.</param>
    public void AddComponent<T>(T component) where T : IComponent
    {
        _components[typeof(T)] = component;
    }

    /// <summary>
    /// Adds a component to the entity, or replaces the existing component of the same type.
    /// </summary>
    /// <param name="component">The component to add or replace.</param>
    public void AddOrReplaceComponent(IComponent component)
    {
        var type = component.GetType();
        _components[type] = component;
    }

    /// <summary>
    /// Try to get a component from the entity.
    /// </summary>
    /// <typeparam name="T">The type of the component to get.</typeparam>
    /// <param name="component">The component to get.</param>
    /// <returns>True if the component was found, false otherwise.</returns>
    public bool TryGet<T>(out T component) where T : class, IComponent
    {
        if (_components.TryGetValue(typeof(T), out var value))
        {
            component = value as T;
            return true;
        }

        component = null;
        return false;
    }

    /// <summary>
    /// Check if the entity has a component of the given type.
    /// </summary>
    /// <typeparam name="T">The type of the component to check for.</typeparam>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool Has<T>() where T : IComponent
    {
        return _components.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Remove a component from the entity.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    public void Remove<T>() where T : IComponent
    {
        _components.Remove(typeof(T));
    }

    /// <summary>
    /// Get all components of the entity.
    /// </summary>
    /// <returns>An enumerable of all components of the entity.</returns>
    public IEnumerable<IComponent> GetAllComponents()
    {
        return _components.Values;
    }
}