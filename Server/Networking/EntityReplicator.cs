using LiteNetLib;
using LiteNetLib.Utils;
using Shared.ECS;
using Shared.Networking;

namespace Server.Networking;

/// <summary>
/// Handles serialization and replication of ECS entities and their components to network peers.
/// 
/// <para>
/// The <see cref="EntityReplicator"/> is responsible for creating a snapshot of all entities
/// marked with <see cref="ReplicatedEntityComponent"/> and serializing their state for network transmission.
/// Only components implementing <see cref="ISerializableComponent"/> are included in the snapshot.
/// </para>
/// 
/// <para>
/// This class is a core part of the server's replication layer, ensuring that all connected clients
/// receive consistent and up-to-date game state.
/// </para>
/// </summary>
public class EntityReplicator
{
    private readonly EntityRegistry _entityManager;

    /// <summary>
    /// Constructs a new <see cref="EntityReplicator"/> for the given entity registry.
    /// </summary>
    /// <param name="entityManager">The entity registry to replicate entities from.</param>
    public EntityReplicator(EntityRegistry entityManager)
    {
        _entityManager = entityManager;
    }

    /// <summary>
    /// Sends a full snapshot of all replicated entities and their serializable components to all peers.
    /// </summary>
    /// <param name="peers">The network peers to send the snapshot to.</param>
    public void SendSnapshotToAll(IEnumerable<NetPeer> peers)
    {
        var snapshot = CreateSnapshot();
        foreach (var peer in peers)
        {
            SendSnapshotTo(peer, snapshot);
        }
    }

    /// <summary>
    /// Creates a binary snapshot of all entities with <see cref="ReplicatedEntityComponent"/>,
    /// including all components implementing <see cref="ISerializableComponent"/>.
    /// </summary>
    /// <returns>A byte array containing the serialized snapshot.</returns>
    private byte[] CreateSnapshot()
    {
        var writer = new NetDataWriter();

        var entities = _entityManager
            .GetAll()
            .Where(e => e.Has<ReplicatedEntityComponent>())
            .ToList();
        
        writer.Put(entities.Count);

        foreach (var entity in entities)
        {
            writer.Put(entity.Id.Value.ToString());

            var serializableComponents = entity
                .GetAllComponents()
                .Where(x => x is ISerializableComponent)
                .Cast<ISerializableComponent>()
                .ToList();
            
            writer.Put(serializableComponents.Count);
            foreach (var component in serializableComponents)
            {
                // For simplicity, serialize the component type name
                writer.Put(component.GetType().FullName);
                component.Serialize(writer);
            }
        }

        return writer.CopyData();
    }

    /// <summary>
    /// Sends a serialized snapshot to a specific network peer.
    /// </summary>
    /// <param name="peer">The peer to send the snapshot to.</param>
    /// <param name="snapshotData">The serialized snapshot data.</param>
    private void SendSnapshotTo(NetPeer peer, byte[] snapshotData)
    {
        peer.Send(snapshotData, DeliveryMethod.ReliableOrdered);
    }
}