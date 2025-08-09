using Shared.ECS.Components;
using Shared.ECS.Replication;

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