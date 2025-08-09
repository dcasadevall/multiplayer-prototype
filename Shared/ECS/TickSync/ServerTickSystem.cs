using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Logging;

namespace Shared.ECS.TickSync
{
    /// <summary>
    /// ServerTickSystem provides the current server tick number to the client via
    /// a replicated entity.
    /// This is used to synchronize the World Tick across clients and the server.
    /// </summary>
    public class ServerTickSystem : ISystem
    {
        private readonly TickSync _tickSync;
        private Entity? _serverTickEntity;

        public ServerTickSystem(TickSync tickSync)
        {
            _tickSync = tickSync;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Get or create the server tick entity
            if (_serverTickEntity == null)
            {
                _serverTickEntity = registry.CreateEntity();
            }

            _serverTickEntity.AddOrReplaceComponent(new ServerTickComponent { TickNumber = tickNumber });

            // On the server, we set both the client and server tick to the same value.
            // There is no "offset" on the server, as it is the authoritative source of truth.
            _tickSync.ClientTick = tickNumber;
            _tickSync.ServerTick = tickNumber;
        }
    }
}