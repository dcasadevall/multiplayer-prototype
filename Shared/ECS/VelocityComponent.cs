using System.Numerics;

namespace Shared.ECS;

/// <summary>
/// Stores the 3D velocity of an entity.
/// </summary>
public class VelocityComponent : IComponent
{
    public Vector3 Value;
    public VelocityComponent() { }
    public VelocityComponent(Vector3 value) => Value = value;
}