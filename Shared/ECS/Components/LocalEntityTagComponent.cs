namespace Shared.ECS.Components
{
    /// <summary>
    /// A tag component to mark an entity as being purely local to this client.
    /// This is used to prevent the replication system from destroying it.
    /// </summary>
    public class LocalEntityTagComponent : IComponent
    {
    }
}