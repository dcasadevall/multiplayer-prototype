using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Health;
using Shared.Player;
using Xunit;

namespace SharedUnitTests.Player
{
    public class PlayerDeathSystemTests
    {
        [Fact]
        public void Update_DestroysDeadPlayerAndCreatesDeathRecord()
        {
            var registry = new EntityRegistry();
            var system = new PlayerDeathSystem();

            var player = registry.CreateEntity();
            player.AddComponent(new PlayerTagComponent());
            player.AddComponent(new HealthComponent(100) { CurrentHealth = 0 });

            system.Update(registry, 42, 0.016f);

            Assert.False(registry.TryGet(player.Id, out _)); // player destroyed

            var deadPlayers = registry.With<DeadPlayerComponent>().ToList();
            Assert.Single(deadPlayers);
            Assert.Equal(42U, deadPlayers[0].GetRequired<DeadPlayerComponent>().DiedAtTick);
        }

        [Fact]
        public void Update_DoesNotCreateDeathRecordTwice()
        {
            var registry = new EntityRegistry();
            var system = new PlayerDeathSystem();

            var player = registry.CreateEntity();
            player.AddComponent(new PlayerTagComponent());
            player.AddComponent(new HealthComponent(100) { CurrentHealth = 0 });

            system.Update(registry, 1, 0.016f);
            system.Update(registry, 2, 0.016f);

            var deadPlayers = registry.With<DeadPlayerComponent>().ToList();
            Assert.Single(deadPlayers);
        }
    }
}