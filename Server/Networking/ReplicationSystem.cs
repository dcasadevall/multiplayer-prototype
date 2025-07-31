using LiteNetLib;
using Shared.ECS;
using Shared.ECS.Simulation;

namespace Server.Networking;

/// <summary>
/// ECS system responsible for replicating the current world state to all connected clients on a fixed interval.
/// 
/// <para>
/// This system uses a snapshot-based replication approach for simplicity and reliability. Since the world tick is
/// guaranteed to be sequential and deterministic, we can safely broadcast the full state of all replicated entities
/// at regular intervals. This ensures all clients remain synchronized with the authoritative server state.
/// </para>
/// 
/// <para>
/// The <see cref="ReplicationSystem"/> creates and manages an <see cref="EntityReplicator"/>, which serializes
/// all entities marked with <c>ReplicatedEntityComponent</c> and their <c>ISerializableComponent</c> data.
/// Snapshots are sent to all connected peers using reliable, ordered delivery.
/// </para>
/// </summary>
[TickInterval(30)] // Replicate every second (30 ticks at 30Hz)
public class ReplicationSystem : ISystem
{
    private readonly NetManager _netManager;
    private readonly EntityReplicator _replicator;

    /// <summary>
    /// Constructs a new <see cref="ReplicationSystem"/> for the given network manager.
    /// </summary>
    /// <param name="netManager">The LiteNetLib network manager for sending snapshots.</param>
    /// <param name="entityRegistry">The entity registry containing all entities and components.</param>
    public ReplicationSystem(NetManager netManager, EntityRegistry entityRegistry)
    {
        _netManager = netManager;
        _replicator = new EntityReplicator(entityRegistry);
    }

    /// <summary>
    /// Called by the world on each eligible tick to replicate the current state to all clients.
    /// Initializes the <see cref="EntityReplicator"/> if needed and sends a full snapshot to all connected peers.
    /// </summary>
    /// <param name="registry">The entity registry containing all entities and components.</param>
    /// <param name="tickNumber">The current world tick number (sequential and deterministic).</param>
    /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
    public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
    {
        Console.WriteLine("ReplicationSystem: Sending snapshot to all clients...");
        _replicator.SendSnapshotToAll(_netManager.ConnectedPeerList);
    }
}