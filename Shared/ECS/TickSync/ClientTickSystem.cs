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
        // How much we allow the client tick to drift ahead of the server tick before we stall.
        private const uint DriftTolerance = 10;

        // Buffer added on top of the ping-based target offset.
        private const uint TickBuffer = 2;

        // How much we smooth the client tick per frame.
        private const uint TickSmoothAmount = 2;

        private readonly TickSync _tickSync;
        private readonly IClientConnection _connection;

        /// <summary>
        /// Constructs a new <see cref="ClientTickSystem"/>.
        /// </summary>
        /// <param name="tickSync">The tick synchronization state to update.</param>
        /// <param name="connection">The client connection used to retrieve ping and other network state.</param>
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
            // Update the client tick number to the current tick.
            // This is useful for classes outside the ECS that need to know the current tick.
            _tickSync.ClientTick = tickNumber;

            // Find the entity that has the ServerTickComponent.
            var tickEntity = registry.GetAll().FirstOrDefault(x => x.Has<ServerTickComponent>());
            if (tickEntity == null) return;
            var serverTickComponent = tickEntity.GetRequired<ServerTickComponent>();

            // Update the server tick and smooth it for interpolation.
            _tickSync.ServerTick = serverTickComponent.TickNumber;
            _tickSync.SmoothedTick = Lerping.Lerp(_tickSync.SmoothedTick, _tickSync.ServerTick, 0.1f);

            // If we are not initialized, set the client tick to the server tick
            if (!_tickSync.IsInitialized)
            {
                _tickSync.ClientTick = _tickSync.ServerTick;
            }

            // Correct for drift based on the current ping and fixed delta time.
            CorrectForDrift();
        }

        private void CorrectForDrift()
        {
            // A. Calculate the ideal offset from the server based on latency.
            var pingInSeconds = _connection.PingMs / 1000.0f;
            var targetOffset = (int)(pingInSeconds / SharedConstants.FixedDeltaTime.TotalSeconds) + TickBuffer;

            // B. Get our current, real offset.
            var currentOffset = (int)_tickSync.ClientTick - (int)_tickSync.ServerTick;

            // C. Nudge the clock to stay within the target offset "sweet spot".
            if (currentOffset < targetOffset)
            {
                // We are too close to the server. Speed up to hide more latency.
                _tickSync.ClientTick += TickSmoothAmount;
            }
            else if (currentOffset > targetOffset + DriftTolerance)
            {
                // We are too far ahead. Stall for one frame to let the server catch up.
                // We do not increment the clock this frame.
            }
            else
            {
                // We are in the sweet spot. Normal operation.
                _tickSync.ClientTick++;
            }
        }
    }
}