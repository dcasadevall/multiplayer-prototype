namespace Shared.ECS;

/// <summary>
///     Interface for all ECS systems. Systems contain logic and operate on entities with specific components.
/// </summary>
public interface ISystem
{
    void Update(EntityManager manager, float deltaTime);
}