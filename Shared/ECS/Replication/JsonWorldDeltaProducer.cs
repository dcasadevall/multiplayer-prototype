using System.Linq;
using Shared.ECS.Entities;
using Shared.Logging;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// WorldDeltaProducer is responsible for creating a delta of the ECS world.
    /// It uses Json serialization and UTF8 encoding to serialize entities and their components.
    /// </summary>
    public class JsonWorldDeltaProducer : IWorldDeltaProducer
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructs a new <see cref="JsonWorldDeltaProducer"/> for the given entity registry.
        /// </summary>
        /// <param name="entityRegistry">The entity registry to replicate entities from.</param>
        /// <param name="logger">Logger for debugging serialization issues.</param>
        public JsonWorldDeltaProducer(EntityRegistry entityRegistry, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Creates a delta of all entities with <see cref="ReplicatedTagComponent"/> that have changed since the last delta,
        /// including all components.
        /// Internally, uses the <see cref="EntityRegistry"/> to generate the diff.
        /// 
        /// <para>
        /// The delta includes the entity ID and a list of components with their type and serialized JSON state.
        /// </para>
        ///
        /// NOTE: This implementation does not scale well for large worlds or many entities.
        /// We should consider moving off of dynamic typing and JSON serialization as needed.
        /// </summary>
        /// <returns>The delta message containing all entities in the world</returns>
        public WorldDeltaMessage ProduceDelta()
        {
            return new WorldDeltaMessage
            {
                Deltas = _entityRegistry.ProduceEntityDelta()
            };
        }
    }
}