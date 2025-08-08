using Shared.ECS;

namespace Core.Physics
{
    /// <summary>
    /// A client-only tag component that marks an entity's bounds to be rendered for debugging.
    /// The <see cref="BoundingBoxRenderSystem"/> will only visualize entities that have this component
    /// in addition to a <see cref="Shared.Physics.BoundingBoxComponent"/>.
    /// </summary>
    public class RenderBoundsTagComponent : IComponent
    {
    }
}

