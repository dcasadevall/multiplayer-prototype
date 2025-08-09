using System;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.Logging;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// JSON Implementation of <see cref="IWorldDeltaConsumer"/>.
    /// <para>
    /// Deserializes a world delta and applies it to the provided <see cref="EntityRegistry"/>.
    /// For each entity in the delta, components are deserialized and added, modified or removed from in the registry.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This class is typically used on the client to keep the local world state synchronized with the server.
    /// </remarks>
    public class JsonWorldDeltaConsumer : IWorldDeltaConsumer
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly ILogger _logger;

        public JsonWorldDeltaConsumer(EntityRegistry entityRegistry, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _logger = logger;
        }

        /// <inheritdoc />
        public void ConsumeDelta(WorldDeltaMessage deltaMessage)
        {
            foreach (var delta in deltaMessage.Deltas)
            {
                var entity = _entityRegistry.GetOrCreate(delta.EntityId);

                foreach (var component in delta.AddedOrModifiedComponents)
                {
                    if (entity.TrySetServerAuthoritativeValue(component.GetType(), component))
                    {
                        _logger.Debug(LoggedFeature.Replication,
                            "Entity {0} has predicted component {1}. Setting server authoritative value.",
                            entity.Id, component.GetType().Name);

                        if (!entity.Has(component.GetType()))
                        {
                            entity.AddComponent(component, component.GetType());
                        }
                    }
                    else
                    {
                        entity.AddOrReplaceComponent(component);
                    }
                }

                foreach (var componentType in delta.RemovedComponents)
                {
                    entity.Remove(componentType);
                }
            }
        }
    }
}