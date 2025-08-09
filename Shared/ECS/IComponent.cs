namespace Shared.ECS
{
    /// <summary>
    /// Base interface for all components in the Entity-Component-System (ECS) architecture.
    /// 
    /// <para>
    /// Components are pure data containers that hold information about entities. They should not
    /// contain any logic or behavior. Systems operate on entities that have specific sets of components.
    /// </para>
    /// 
    /// <example>
    /// <code>
    /// public class PositionComponent : IComponent
    /// {
    ///     public Vector2 Value;
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public interface IComponent
    {
    }
}