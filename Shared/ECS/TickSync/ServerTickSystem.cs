using Shared.ECS.Entities;
using Shared.ECS.Replication;

namespace Shared.ECS.TickSync
{
    /// <summary>
    /// ServerTickSystem provides the current server tick number to the client via
    /// a replicated entity.
    /// This is used to synchronize the World Tick across clients and the server.
    /// </summary>
    public class ServerTickSystem : ISystem
    {
        private Entity? _serverTickEntity;

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Get or create the server tick entity
            if (_serverTickEntity == null)
            {
                _serverTickEntity = registry.CreateEntity();
                _serverTickEntity.AddComponent(new ReplicatedTagComponent());
            }

            _serverTickEntity.AddOrReplaceComponent(new ServerTickComponent { TickNumber = tickNumber });
        }
    }
}