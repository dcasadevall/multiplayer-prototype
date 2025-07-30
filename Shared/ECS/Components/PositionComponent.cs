using System.Numerics;

namespace Shared.ECS.Components;

/// <summary>
/// Stores the 3D position of an entity.
/// </summary>
public class PositionComponent : IComponent
{
    public Vector3 Value;

    public PositionComponent()
    {
    }

    public PositionComponent(Vector3 value)
    {
        Value = value;
    }
}