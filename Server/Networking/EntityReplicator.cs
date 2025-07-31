using System.Text;
using System.Text.Json;
using LiteNetLib;
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
    private readonly EntityRegistry _entityRegistry;

    /// <summary>
    /// Constructs a new <see cref="EntityReplicator"/> for the given entity registry.
    /// </summary>
    /// <param name="entityRegistry">The entity registry to replicate entities from.</param>
    public EntityReplicator(EntityRegistry entityRegistry)
    {
        _entityRegistry = entityRegistry;
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
    /// Internally, uses JSON serialization to convert component states into a format suitable for network transmission.
     /// 
     /// <para>
     /// The snapshot includes the entity ID and a list of components with their type and serialized JSON state.
     /// </para>
     ///
     /// NOTE: This implementation does not scale well for large worlds or many entities.
     /// We should consider moving off of dynamic typing and JSON serialization as needed.
    /// </summary>
    /// <returns>A byte array containing the serialized snapshot.</returns>
    private byte[] CreateSnapshot()
    {
        var snapshot = new WorldSnapshotMessage();

        foreach (var entity in _entityRegistry.GetAll().Where(e => e.Has<ReplicatedEntityComponent>()))
        {
            var components = entity.GetAllComponents()
                .Where(component => component is ISerializableComponent)
                .Select(component => new SnapshotComponent
                {
                    Type = component.GetType().FullName!,
                    Json = JsonSerializer.Serialize(component, component.GetType())
                })
                .ToList();

            snapshot.Entities.Add(new SnapshotEntity
            {
                Id = entity.Id.Value,
                Components = components
            });
        }

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
    }

    /// <summary>
    /// Sends a serialized snapshot to a specific network peer.
    /// </summary>
    /// <param name="peer">The peer to send the snapshot to.</param>
    /// <param name="snapshotData">The serialized snapshot data.</param>
    private void SendSnapshotTo(NetPeer peer, byte[] snapshotData)
    {
        peer.Send(snapshotData, DeliveryMethod.Unreliable);
    }
}