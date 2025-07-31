namespace Shared.Networking.Replication;

/// <summary>
/// Handles serialization of ECS entities and their components.
/// 
/// <para>
/// The <see cref="JsonWorldSnapshotProducer"/> is responsible for creating a snapshot of all entities
/// marked with <see cref="ReplicatedTagComponent"/> and serializing their state for network transmission.
/// </para>
///
/// <para>
/// Implementations are responsible for producing a byte array that represents the current state of the world.
/// The format of the snapshot is implementation-specific.
/// </para>
/// </summary>
public interface IWorldSnapshotProducer
{
    /// <summary>
    /// Creates a binary snapshot of all entities with <see cref="ReplicatedTagComponent"/>,
    /// including all components.
    /// </summary>
    /// <returns>A byte array containing the serialized snapshot.</returns>
    byte[] ProduceSnapshot();
}
