using System;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Simulation;

namespace Shared.Player
{
    /// <summary>
    /// System that handles player respawn logic.
    /// <b>This system is server-only.</b>
    /// It waits for a configured respawn time after death, then respawns the player at a random position.
    /// </summary>
    public class PlayerRespawnSystem : ISystem
    {
        private readonly Random _rand = new();

        /// <summary>
        /// Checks for dead players whose respawn time has elapsed, destroys their death record,
        /// and respawns them at a random position.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">Current simulation tick.</param>
        /// <param name="deltaTime">Delta time for the tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var deadPlayers = registry.With<DeadPlayerComponent>().ToList();
            foreach (var player in deadPlayers)
            {
                var deadComponent = player.GetRequired<DeadPlayerComponent>();

                // Wait until the respawn time has passed
                if (tickNumber <= deadComponent.DiedAtTick + GameplayConstants.PlayerRespawnTime.ToNumTicks()) continue;

                // Destroy the dead player entity
                var peerId = deadComponent.PeerId;
                registry.DestroyEntity(player.Id);

                // Respawn the player at a random position within a small area
                PlayerArchetype.Create(
                    registry,
                    peerId,
                    new System.Numerics.Vector3(_rand.Next(-3, 3), 0, _rand.Next(-3, 3)));
            }
        }
    }
}