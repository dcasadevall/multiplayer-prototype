using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;

namespace Shared.Respawn
{
    public static class RespawnExtensions
    {
        /// <summary>
        /// Tries to get the respawning player entity for the given peer ID.
        /// </summary>
        /// <param name="entityRegistry"></param>
        /// <param name="peerId"></param>
        /// <returns></returns>
        public static Entity? GetRespawningPlayer(this EntityRegistry entityRegistry, int peerId)
        {
            return entityRegistry
                .GetAll()
                .Where(x => x.Has<RespawnComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == peerId);
        }
    }
}