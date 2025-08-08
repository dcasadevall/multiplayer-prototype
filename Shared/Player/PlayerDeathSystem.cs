using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Health;

namespace Shared.Player
{
    /// <summary>
    /// System that handles player death logic.
    /// Runs on both server and client.
    /// Destroys dead player entities and creates death records for respawn logic.
    /// </summary>
    public class PlayerDeathSystem : ISystem
    {
        /// <summary>
        /// Checks for dead players, destroys their entities, and creates death records.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">Current simulation tick.</param>
        /// <param name="deltaTime">Delta time for the tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var deadPlayers = registry
                .WithAll<PlayerTagComponent, HealthComponent>()
                .Where(e => e.GetRequired<HealthComponent>().CurrentHealth <= 0 && !e.Has<DeadPlayerComponent>())
                .ToList();

            foreach (var player in deadPlayers)
            {
                // Destroy the player entity. This "Kills" the player and all its associated components.
                registry.DestroyEntity(player.Id);

                // Create a "dead player" entity to track death state for respawn logic.
                var deadPlayer = registry.CreateEntity();
                var peerComponent = player.GetRequired<PeerComponent>();
                deadPlayer.AddComponent(new DeadPlayerComponent { DiedAtTick = tickNumber, PeerId = peerComponent.PeerId });
            }
        }
    }
}