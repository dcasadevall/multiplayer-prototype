using System.Linq;
using Shared.Math;

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

        /// <summary>
        /// Constructs a new <see cref="ClientTickSystem"/>.
        /// </summary>
        /// <param name="tickSync">The tick synchronization state to update.</param>
        public ClientTickSystem(TickSync tickSync)
        {
            _tickSync = tickSync;
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

            var serverTick = tickEntity.GetRequired<ServerTickComponent>();

            int estimatedLatencyTicks = EstimateLatencyInTicks(); // based on RTT and tick rate
            _tickSync.ClientTick = serverTick.TickNumber + (uint)estimatedLatencyTicks;
            _tickSync.ServerTick = serverTick.TickNumber;
            _tickSync.SmoothedTick = Lerping.Lerp(_tickSync.SmoothedTick, _tickSync.ServerTick, 0.1f);

            if (!_tickSync.IsInitialized)
            {
                _tickSync.TickOffset = (int)(tickNumber - serverTick.TickNumber);
                _tickSync.IsInitialized = true;
            }
        }

        private int EstimateLatencyInTicks()
        {
            // This is a placeholder for actual latency estimation logic.
            // In a real implementation, we would measure round-trip time (RTT)
            // and convert it to ticks based on the tick rate.
            // For example, if our tick rate is 30 ticks per second,
            // and RTT is 100ms, then:
            // RTT in seconds = 0.1
            // Ticks = RTT * TickRate = 0.1 * 30 = 3 ticks
            return 3;
        }
    }
}