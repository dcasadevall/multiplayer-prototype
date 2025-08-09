using Shared.ECS.Entities;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// Consumes and applies world state deltas received from the network.
    /// <para>
    /// Implementations of this interface are responsible for deserializing a delta (typically received from the server)
    /// and applying it to the local <see cref="EntityRegistry"/>. This is a core part of the client-side replication system,
    /// ensuring the client's world state is kept in sync with the authoritative server.
    /// </para>
    /// </summary>
    public interface IWorldDeltaConsumer
    {
        /// <summary>
        /// Consumes a serialized world delta and applies it to the local entity registry.
        /// </summary>
        /// <param name="delta">The serialized delta message.</param>
        void ConsumeDelta(WorldDeltaMessage delta);
    }
}