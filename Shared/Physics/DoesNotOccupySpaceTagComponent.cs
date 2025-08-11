using Shared.ECS;
using Shared.ECS.Components;

namespace Shared.Physics
{
    /// <summary>
    /// Tag component indicating that this entity does not occupy space for the purposes of unit separation.
    /// Entities with this tag will still participate in collision detection (for hits/damage),
    /// but the <see cref="UnitCollisionSystem"/> will not push them or push other entities away from them.
    ///
    /// Note: This is a stopgap solution. The ideal approach is a configurable collision matrix in settings
    /// that defines which categories collide or resolve against which others (e.g., Units vs. Projectiles, Units vs. Units, etc.).
    /// </summary>
    public class DoesNotOccupySpaceTagComponent : TagComponent
    {
    }
}
