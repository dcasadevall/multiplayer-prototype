using System.Numerics;

namespace Shared.ECS.Components;

/// <summary>
///     Stores the 3D velocity of an entity.
/// </summary>
public class VelocityComponent : IComponent
{
    public Vector3 Value;
}