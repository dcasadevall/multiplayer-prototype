using System.Linq;
using LiteNetLib;
using Shared.ECS.Components;
using Shared.Player;

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

        /// <summary>
        /// Tries to get the dead player entity for the given peer ID.
        /// </summary>
        /// <param name="entityRegistry"></param>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public static Entity? GetDeadPlayerEntity(this EntityRegistry entityRegistry, int peerId)
        {
            return entityRegistry
                .GetAll()
                .Where(x => x.Has<DeadPlayerComponent>())
                .FirstOrDefault(x => x.GetRequired<DeadPlayerComponent>().PeerId == peerId);
        }
    }
}