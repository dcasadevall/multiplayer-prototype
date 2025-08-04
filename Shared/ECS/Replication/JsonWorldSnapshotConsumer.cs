using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Shared.ECS.Prediction;
using Shared.Logging;

namespace Shared.ECS.Replication
{
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

                // Remove components that are not in the snapshot
                var currentComponents = entity.GetAllComponents().ToList();
                var componentSet = snapshotEntity.Components.Select(x => x.Type).ToHashSet();
                foreach (var component in currentComponents)
                {
                    if (!componentSet.Contains(component.GetType().FullName))
                    {
                        _logger.Debug("Removing component {0} from entity {1}", component.GetType().Name, entity.Id);
                        entity.Remove(component.GetType());
                    }
                }

                // Add or replace components from the snapshot
                foreach (var component in snapshotEntity.Components)
                {
                    var componentType = Type.GetType(component.Type);
                    if (componentType == null)
                    {
                        continue;
                    }

                    var componentInstance = JsonSerializer.Deserialize(component.Json, componentType);
                    if (componentInstance == null)
                    {
                        continue;
                    }

                    entity.AddOrReplaceComponent((IComponent)componentInstance);

                    // If the entity has a predicted component of this type, we only
                    // deserialize the server value.
                    // We will only deserialize the component value if this is the first time
                    // we are receiving this component.
                    if (entity.TrySetServerAuthoritativeValue(componentType, (IComponent)componentInstance))
                    {
                        _logger.Debug("Entity {0} has predicted component {1}. Setting server authoritative value.",
                            entity.Id, componentType.Name);

                        // Only add the predicted component if it doesn't already exist
                        // This prevents overwriting existing predicted components
                        if (!entity.Has(componentType))
                        {
                            entity.AddComponent((IComponent)componentInstance, componentType);
                        }
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