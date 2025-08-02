using System.Linq;
using System.Text;
using System.Text.Json;
using Shared.Logging;

namespace Shared.ECS.Replication
{
    /// <summary>
    /// WorldSnapshotProducer is responsible for creating a snapshot of the ECS world.
    /// It uses Json serialization and UTF8 encoding to serialize entities and their components.
    /// </summary>
    public class JsonWorldSnapshotProducer : IWorldSnapshotProducer
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructs a new <see cref="JsonWorldSnapshotProducer"/> for the given entity registry.
        /// </summary>
        /// <param name="entityRegistry">The entity registry to replicate entities from.</param>
        /// <param name="logger">Logger for debugging serialization issues.</param>
        public JsonWorldSnapshotProducer(EntityRegistry entityRegistry, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Creates a binary snapshot of all entities with <see cref="ReplicatedTagComponent"/>,
        /// including all components.
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
        public byte[] ProduceSnapshot()
        {
            var snapshot = new WorldSnapshotMessage();
            var replicatedEntities = _entityRegistry.GetAll().Where(e => e.Has<ReplicatedTagComponent>()).ToList();
            _logger.Debug("Found {0} entities to replicate", replicatedEntities.Count);

            foreach (var entity in replicatedEntities)
            {
                var components = entity.GetAllComponents()
                    .Where(component => component.GetType() != typeof(ReplicatedTagComponent))
                    .Select(component => new SnapshotComponent
                    {
                        Type = component.GetType().FullName!,
                        Json = JsonSerializer.Serialize(component, component.GetType(), new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
                    })
                    .ToList();

                _logger.Debug("Entity {0} has {1} components to replicate", entity.Id, components.Count);

                snapshot.Entities.Add(new SnapshotEntity
                {
                    Id = entity.Id.Value,
                    Components = components
                });
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true // Makes debugging easier
            };

            var json = JsonSerializer.Serialize(snapshot, options);
            _logger.Debug("Produced snapshot JSON: {0}", json);

            return Encoding.UTF8.GetBytes(json);
        }
    }
}