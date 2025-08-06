using System.Linq;

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
            // The client's world tick is the source of truth for the ClientTick.
            _tickSync.ClientTick = tickNumber;

            var tickEntity = registry.GetAll().FirstOrDefault(x => x.Has<ServerTickComponent>());
            if (tickEntity == null) return;

            var serverTickComponent = tickEntity.GetRequired<ServerTickComponent>();

            // Update the server tick and smooth it for interpolation.
            _tickSync.ServerTick = serverTickComponent.TickNumber;
        }
    }
}