namespace Shared.ECS;

/// <summary>
/// Marker interface for all ECS components.
/// <para>
/// In the Entity-Component-System (ECS) architecture, a <b>Component</b> is a simple data container
/// that holds state for an entity. Components should be as granular as possible and contain no logic—
/// only data. Systems operate on entities by querying for specific component types.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class PositionComponent : IComponent
/// {
///     public Vector3 Value;
/// }
/// </code>
/// </para>
/// </summary>
public interface IComponent
{
}