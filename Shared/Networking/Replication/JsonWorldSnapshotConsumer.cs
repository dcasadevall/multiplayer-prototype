using System.Text;
using System.Text.Json;
using Shared.ECS;
using Shared.Logging;
using System;
using System.Linq;

namespace Shared.Networking.Replication;

/// <summary>
/// JSON Implementation of <see cref="IWorldSnapshotConsumer"/>.
/// <para>
/// Deserializes a world snapshot (in JSON format) and applies it to the provided <see cref="EntityRegistry"/>.
/// For each entity in the snapshot, components are deserialized and added or replaced in the registry.
/// Entities not present in the snapshot are pruned from the registry to maintain consistency with the server.
/// </para>
/// </summary>
/// <remarks>
/// This class is typically used on the client to keep the local world state synchronized with the server.
/// </remarks>
public class JsonWorldSnapshotConsumer(EntityRegistry entityRegistry, ILogger logger) : IWorldSnapshotConsumer
{
    /// <summary>
    /// Consumes a JSON-encoded world snapshot and updates the local entity registry.
    /// </summary>
    /// <param name="snapshot">The snapshot data as a UTF-8 encoded JSON byte array.</param>
    public void ConsumeSnapshot(byte[] snapshot)
    {
        // If the snapshot is empty, do nothing
        if (snapshot.Length == 0)
        {
            return;
        }
        
        var json = Encoding.UTF8.GetString(snapshot);
        var snapshotMsg = JsonSerializer.Deserialize<WorldSnapshotMessage>(json);
        if (snapshotMsg == null)
        {
            return;
        }

        foreach (var snapshotEntity in snapshotMsg.Entities)
        {
            var entity = entityRegistry.GetOrCreate(snapshotEntity.Id);
            
            foreach (var component in snapshotEntity.Components)
            {
                var componentType = Type.GetType(component.Type);
                if (componentType == null) 
                {
                    continue;
                }

                var componentInstance = JsonSerializer.Deserialize(component.Json, componentType);
                if (componentInstance != null)
                {
                    entity.AddOrReplaceComponent((IComponent)componentInstance);
                }
            }
        }
    }

    /// <summary>
    /// Removes entities from the registry that are not present in the latest snapshot.
    /// </summary>
    /// <param name="entities">The set of entity IDs present in the snapshot.</param>
    private void PruneStaleEntities(IEnumerable<EntityId> entities)
    {
        // Get all current entities in the registry
        var currentEntities = entityRegistry.GetAll().Select(e => e.Id);
        // Find entities that are not in the snapshot
        var staleEntities = currentEntities.Except(entities).ToList();
        // Remove stale entities from the registry
        foreach (var staleId in staleEntities)
        {
            entityRegistry.DestroyEntity(staleId);
        }
    }
}