using Shared.ECS;
using Shared.ECS.Components;

namespace Core.Physics
{
    /// <summary>
    /// A client-only tag component that marks an entity's bounds to be rendered for debugging.
    /// The <see cref="WorldAABBRenderSystem"/> will only visualize entities that have this component
    /// in addition to a <see cref="Shared.Physics.WorldAABBComponent"/>.
    /// </summary>
    public class RenderBoundsTagComponent : TagComponent
    {
    }
}

