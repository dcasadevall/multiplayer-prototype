using System.Linq;
using Shared.Math;
using Shared.Networking;

namespace Shared.ECS.TickSync
{
    /// <summary>
    /// System responsible for synchronizing the client's tick state with the server's tick.
    /// 
    /// This system should only be injected and used in the client world.
    /// On each update, it reads the latest <see cref="ServerTickComponent"/> from the entity registry,
    /// updates the <see cref="TickSync"/> structure with the current client and server tick numbers,
    /// and applies smoothing to the tick difference for interpolation or prediction purposes.
    /// </summary>
    public class ClientTickSystem : ISystem
    {
        private readonly TickSync _tickSync;
        private readonly IClientConnection _connection;

        /// <summary>
        /// Constructs a new <see cref="ClientTickSystem"/>.
        /// </summary>
        /// <param name="tickSync">The tick synchronization state to update.</param>
        /// <param name="connection">The client connection used to retrieve ping information.</param>
        public ClientTickSystem(TickSync tickSync, IClientConnection connection)
        {
            _tickSync = tickSync;
            _connection = connection;
        }

        /// <summary>
        /// Updates the tick synchronization state based on the latest <see cref="ServerTickComponent"/> component.
        /// </summary>
        /// <param name="registry">The entity registry to query for tick information.</param>
        /// <param name="tickNumber">The current client tick number.</param>
        /// <param name="deltaTime">The time elapsed since the last tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var tickEntity = registry.GetAll().FirstOrDefault(x => x.Has<ServerTickComponent>());
            if (tickEntity == null) return;

            var serverTickComponent = tickEntity.GetRequired<ServerTickComponent>();

            // Update the server tick and smooth it for interpolation.
            _tickSync.ServerTick = serverTickComponent.TickNumber;
            _tickSync.SmoothedTick = Lerping.Lerp(_tickSync.SmoothedTick, _tickSync.ServerTick, 0.1f);

            // Update the client tick, accounting for latency.
            var halfRttInTicks = (uint)(_connection.PingMs / (SharedConstants.WorldTickRate * 1000f));
            _tickSync.ClientTick = _tickSync.ServerTick + halfRttInTicks;
        }
    }
}