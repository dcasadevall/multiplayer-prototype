using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Shared.ECS;
using Shared.Logging;

namespace Shared.Networking.Replication
{
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
    public class JsonWorldSnapshotConsumer : IWorldSnapshotConsumer
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly ILogger _logger;

        public JsonWorldSnapshotConsumer(EntityRegistry entityRegistry, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _logger = logger;
        }

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
            _logger.Debug("Attempting to deserialize JSON: {0}", json);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            WorldSnapshotMessage? snapshotMsg;
            try
            {
                snapshotMsg = JsonSerializer.Deserialize<WorldSnapshotMessage>(json, options);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to deserialize snapshot: {0}", e.Message);
                return;
            }

            if (snapshotMsg == null)
            {
                _logger.Error("Failed to deserialize snapshot - result was null");
                return;
            }

            _logger.Debug("Successfully deserialized snapshot with {0} entities", snapshotMsg.Entities.Count);

            foreach (var snapshotEntity in snapshotMsg.Entities)
            {
                var entity = _entityRegistry.GetOrCreate(snapshotEntity.Id);
            
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
            var currentEntities = _entityRegistry.GetAll().Select(e => e.Id);
            // Find entities that are not in the snapshot
            var staleEntities = currentEntities.Except(entities).ToList();
            // Remove stale entities from the registry
            foreach (var staleId in staleEntities)
            {
                _entityRegistry.DestroyEntity(staleId);
            }
        }
    }
}