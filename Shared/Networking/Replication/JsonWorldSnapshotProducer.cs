using System.Text;
using System.Text.Json;
using Shared.ECS;

namespace Shared.Networking.Replication;

/// <summary>
/// WorldSnapshotProducer is responsible for creating a snapshot of the ECS world.
/// It uses Json serialization and UTF8 encoding to serialize entities and their components.
/// </summary>
public class JsonWorldSnapshotProducer : IWorldSnapshotProducer
{
    private readonly EntityRegistry _entityRegistry;

    /// <summary>
    /// Constructs a new <see cref="JsonWorldSnapshotProducer"/> for the given entity registry.
    /// </summary>
    /// <param name="entityRegistry">The entity registry to replicate entities from.</param>
    public JsonWorldSnapshotProducer(EntityRegistry entityRegistry)
    {
        _entityRegistry = entityRegistry;
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

        foreach (var entity in _entityRegistry.GetAll().Where(e => e.Has<ReplicatedTagComponent>()))
        {
            var components = entity.GetAllComponents()
                .Where(component => component.GetType() != typeof(ReplicatedTagComponent))
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
}