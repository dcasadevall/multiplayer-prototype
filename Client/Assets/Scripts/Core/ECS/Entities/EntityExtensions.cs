using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Physics;

namespace Core.ECS.Entities
{
    public static class EntityExtensions
    {
        public static Entity GetLocalPlayerEntity(this EntityRegistry registry, int assignedPeerId)
        {
            return registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PositionComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == assignedPeerId);
        }
    }
}