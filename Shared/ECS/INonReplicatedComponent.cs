namespace Shared.ECS
{
    /// <summary>
    /// Marker interface for components in the Entity-Component-System (ECS) architecture.
    /// These components should not be serialized or sent to clients.
    ///
    /// We should not use compile time markers for this, but for this
    /// example we keep it simple.
    ///
    /// For prediction, we use PredictionComponent mode to not replicate the server value.
    /// This interface is meant more to avoid unnecessary serialization and pollute
    /// the client with server-only components.
    /// </summary>
    public interface INonReplicatedComponent : IComponent
    {
    }
}