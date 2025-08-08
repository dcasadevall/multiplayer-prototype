using Shared;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using Shared.Player;
using Xunit;

namespace SharedUnitTests.Player
{
    public class PlayerRespawnSystemTests
    {
        [Fact]
        public void Update_RespawnsPlayerAfterRespawnTime()
        {
            var registry = new EntityRegistry();
            var system = new PlayerRespawnSystem();

            var deadPlayer = registry.CreateEntity();
            deadPlayer.AddComponent(new PlayerTagComponent());
            deadPlayer.AddComponent(new DeadPlayerComponent { DiedAtTick = 10, PeerId = 123 });

            // Not enough ticks passed, should not respawn
            system.Update(registry, 11, 0.016f);
            Assert.True(registry.TryGet(deadPlayer.Id, out _));

            // Enough ticks passed, should respawn
            system.Update(registry, 10 + GameplayConstants.PlayerRespawnTime.ToNumTicks() + 1, 0.016f);

            Assert.False(registry.TryGet(deadPlayer.Id, out _)); // dead player destroyed

            var respawned = registry.With<PlayerTagComponent>().ToList();
            Assert.Contains(respawned, e => e.Has<PlayerTagComponent>());
        }
    }
}