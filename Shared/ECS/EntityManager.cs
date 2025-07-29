using System.Collections.Generic;

namespace Shared.ECS;

/// <summary>
/// Manages the lifecycle and lookup of all entities in the ECS world.
/// </summary>
public class EntityManager
{
    private readonly Dictionary<EntityId, Entity> _entities = new();

    public Entity CreateEntity()
    {
        var id = EntityId.New();
        var entity = new Entity(id);
        _entities.Add(id, entity);
        return entity;
    }

    public bool TryGet(EntityId id, out Entity entity) => _entities.TryGetValue(id, out entity);
    public void DestroyEntity(EntityId id) => _entities.Remove(id);
    public IEnumerable<Entity> GetAll() => _entities.Values;
}