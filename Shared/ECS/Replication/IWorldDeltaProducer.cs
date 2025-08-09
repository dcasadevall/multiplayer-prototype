namespace Shared.ECS.Replication
{
    /// <summary>
    /// Handles serialization of ECS entities and their components into deltas.
    /// 
    /// <para>
    /// The <see cref="JsonWorldDeltaProducer"/> is responsible for creating a delta of all entities
    /// marked with <see cref="ReplicatedTagComponent"/> and serializing their state for network transmission.
    /// </para>
    ///
    /// <para>
    /// Implementations are responsible for producing a byte array that represents the changes in the world state since the last delta.
    /// The format of the delta is implementation-specific.
    /// </para>
    /// </summary>
    public interface IWorldDeltaProducer
    {
        /// <summary>
        /// Creates a delta of all entities with <see cref="ReplicatedTagComponent"/> that have changed since the last delta,
        /// including all components.
        /// </summary>
        /// <returns>A message containing the serialized delta.</returns>
        WorldDeltaMessage ProduceDelta();
    }
}