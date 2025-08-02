namespace Shared.ECS.Replication
{
    /// <summary>
    /// Consumes and applies world state snapshots received from the network.
    /// <para>
    /// Implementations of this interface are responsible for deserializing a snapshot (typically received from the server)
    /// and applying it to the local <see cref="EntityRegistry"/>. This is a core part of the client-side replication system,
    /// ensuring the client's world state is kept in sync with the authoritative server.
    /// </para>
    /// </summary>
    public interface IWorldSnapshotConsumer
    {
        /// <summary>
        /// Consumes a serialized world snapshot and applies it to the local entity registry.
        /// </summary>
        /// <param name="snapshot">The serialized snapshot data (e.g., JSON or binary).</param>
        void ConsumeSnapshot(byte[] snapshot);
    }
}