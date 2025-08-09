using System.Collections.Generic;
using System.Linq;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Logging;

namespace Shared.ECS.Systems
{
    /// <summary>
    /// System that handles destroying entities when their TTL expires.
    /// Runs on both client and server to handle temporary entities like projectiles.
    /// </summary>
    public class SelfDestroyingSystem : ISystem
    {
        private readonly ILogger _logger;

        public SelfDestroyingSystem(ILogger logger)
        {
            _logger = logger;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var entitiesToDestroy = registry
                .GetAll()
                .Where(x => x.Has<SelfDestroyingComponent>())
                .Where(x => ShouldDestroy(x.GetRequired<SelfDestroyingComponent>(), tickNumber))
                .ToList();

            foreach (var entity in entitiesToDestroy)
            {
                var selfDestroying = entity.GetRequired<SelfDestroyingComponent>();

                if (!selfDestroying.IsMarkedForDestruction)
                {
                    selfDestroying.IsMarkedForDestruction = true;

                    _logger.Debug(LoggedFeature.Ecs, "Destroying entity {0} at tick {1} (scheduled for {2})",
                        entity.Id, tickNumber, selfDestroying.DestroyAtTick);

                    registry.DestroyEntity(entity.Id);
                }
            }
        }

        private bool ShouldDestroy(SelfDestroyingComponent component, uint currentTick)
        {
            return currentTick >= component.DestroyAtTick && !component.IsMarkedForDestruction;
        }
    }
}