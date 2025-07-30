using LiteNetLib.Utils;
using Shared.ECS;

namespace Server.Networking;

/// <summary>
/// Interface for ECS components that support network serialization and deserialization.
/// 
/// <para>
/// The server's networking and replication layer uses <see cref="ISerializableComponent"/>
/// to mark components whose state should be synchronized across the network.
/// Implementations must provide logic to serialize their state to a <see cref="NetDataWriter"/>
/// and reconstruct it from a <see cref="NetDataReader"/>.
/// </para>
/// 
/// <para>
/// This interface is essential for the replication system, which detects changes to
/// components implementing <see cref="ISerializableComponent"/> and transmits their state
/// to clients, ensuring consistent game state across the network.
/// </para>
/// </summary>
public interface ISerializableComponent : IComponent
{
    /// <summary>
    /// Serializes the component's state to the provided writer for network transmission.
    /// </summary>
    /// <param name="writer">The writer to serialize data into.</param>
    void Serialize(NetDataWriter writer);

    /// <summary>
    /// Deserializes the component's state from the provided reader, reconstructing its state.
    /// </summary>
    /// <param name="reader">The reader to deserialize data from.</param>
    void Deserialize(NetDataReader reader);
}