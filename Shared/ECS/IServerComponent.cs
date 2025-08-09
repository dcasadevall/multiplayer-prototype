namespace Shared.ECS
{
    /// <summary>
    /// Marker interface for server-side components in the Entity-Component-System (ECS) architecture.
    /// These components should not be serialized or sent to clients.
    ///
    /// We should not use compile time markers for this, but for this
    /// example we keep it simple.
    /// Ideally, we would allow devs to specify components that should not be sent to clients
    /// </summary>
    public interface IServerComponent : IComponent
    {
    }
}