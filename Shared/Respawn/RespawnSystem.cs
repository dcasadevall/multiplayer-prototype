using System;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;

namespace Shared.Respawn
{
    /// <summary>
    /// This system is responsible for respawning players and bots.
    /// It processes death records and, when the respawn time is reached,
    /// recreates the player or bot using their respective archetypes.
    /// </summary>
    public class RespawnSystem : ISystem
    {
        private readonly Random _rand = new();

        /// <summary>
        /// Processes death records and respawns entities when their respawn time is reached.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">The current simulation tick.</param>
        /// <param name="deltaTime">The time since the last tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var deadEntities = registry
                .With<RespawnComponent>()
                .Where(e => e.GetRequired<RespawnComponent>().RespawnAtTick <= tickNumber)
                .ToList();

            foreach (var entity in deadEntities)
            {
                var spawnPosition = new System.Numerics.Vector3(_rand.Next(-3, 3), 0, _rand.Next(-3, 3));

                // We identify player vs bot based on the peer component,
                // we may want a more robust way in the future.
                // We could specify the archetype in the RespawnComponent,
                if (entity.Has<PeerComponent>())
                {
                    var peerId = entity.GetRequired<PeerComponent>().PeerId;
                    PlayerArchetype.Create(registry, peerId, spawnPosition);
                }
                else
                {
                    BotArchetype.Create(registry, spawnPosition);
                }

                registry.DestroyEntity(entity.Id);
            }
        }
    }
}