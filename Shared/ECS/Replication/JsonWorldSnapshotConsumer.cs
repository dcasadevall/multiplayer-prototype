using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Shared.ECS.Prediction;
using Shared.Logging;

namespace Shared.ECS.Replication
{
    public class PredictedStateComponent : IComponent
    {
        public Vector3 PredictedPosition;
        public uint LastServerTick;
    }

    /// <summary>
    /// JSON Implementation of <see cref="IWorldSnapshotConsumer"/>.
    /// <para>
    /// Deserializes a world snapshot and applies it to the provided <see cref="EntityRegistry"/>.
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

        /// <inheritdoc />
        public void ConsumeSnapshot(WorldSnapshotMessage snapshot)
        {
            foreach (var snapshotEntity in snapshot.Entities)
            {
                var entity = _entityRegistry.GetOrCreate(snapshotEntity.Id);
                var snapshotComponentTypes = snapshotEntity.Components.Select(c => c.Type).ToHashSet();

                // Remove components that are not in the snapshot
                foreach (var component in entity.GetAllComponents().ToList())
                {
                    if (!snapshotComponentTypes.Contains(component.GetType().FullName) && component is not PredictedStateComponent)
                    {
                        _logger.Debug(LoggedFeature.Replication, "Removing component {0} from entity {1}", component.GetType().Name,
                            entity.Id);
                        entity.Remove(component.GetType());
                    }
                }

                // Add or update components from the snapshot
                foreach (var componentData in snapshotEntity.Components)
                {
                    var componentType = Type.GetType(componentData.Type);
                    if (componentType == null) continue;

                    var deserializedComponent = JsonSerializer.Deserialize(componentData.Json, componentType);
                    if (deserializedComponent == null) continue;
                    var componentInstance = (IComponent)deserializedComponent;

                    // If the entity has a predicted component of this type, we only
                    // deserialize the server value.
                    // We will only deserialize the component value if this is the first time
                    // we are receiving this component.
                    if (entity.TrySetServerAuthoritativeValue(componentType, componentInstance))
                    {
                        _logger.Debug(LoggedFeature.Replication,
                            "Entity {0} has predicted component {1}. Setting server authoritative value.",
                            entity.Id, componentType.Name);

                        // Only add the predicted component if it doesn't already exist
                        // This prevents overwriting existing predicted components
                        if (!entity.Has(componentType))
                        {
                            entity.AddComponent(componentInstance, componentType);
                        }
                    }
                    else
                    {
                        // Only add or replace the component if it is not a predicted component
                        entity.AddOrReplaceComponent(componentInstance);
                    }
                }
            }

            // Prune entities that are not in the snapshot
            PruneStaleEntities(snapshot.Entities.Select(e => new EntityId(e.Id)));
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