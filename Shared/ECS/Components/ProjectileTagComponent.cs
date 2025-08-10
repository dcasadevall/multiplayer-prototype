using Shared.ECS.Components;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Tag component that identifies an entity as a projectile.
    /// Used for systems that need to process projectiles specifically.
    /// </summary>
    public class ProjectileTagComponent : TagComponent
    {
    }
}