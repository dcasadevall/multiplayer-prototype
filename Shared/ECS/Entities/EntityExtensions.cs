using System.Linq;
using Shared.ECS.Components;

namespace Shared.ECS.Entities
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Gets the player entity for the given peer ID.
        /// </summary>
        public static Entity? GetPlayerEntity(this EntityRegistry entityRegistry, int peerId)
        {
            return entityRegistry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == peerId);
        }
    }
}