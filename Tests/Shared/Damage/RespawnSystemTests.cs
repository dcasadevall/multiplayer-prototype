using Shared.ECS;
using Shared.ECS.Components;
using Shared.Respawn;
using Xunit;

namespace SharedUnitTests.Damage
{
    public class RespawnSystemTests
    {
        [Fact]
        public void Update_RespawnsEntityAfterRespawnTime()
        {
            var registry = new EntityRegistry();
            var system = new RespawnSystem();

            var deathRecord = registry.CreateEntity();
            deathRecord.AddComponent(new RespawnComponent { RespawnAtTick = 10 });
            deathRecord.AddComponent(new PlayerTagComponent());
            deathRecord.AddComponent(new PeerComponent { PeerId = 1 });

            system.Update(registry, 10, 0.016f);

            Assert.False(registry.TryGet(deathRecord.Id, out _));
            var players = registry.With<PlayerTagComponent>().ToList();
            Assert.Single(players);
        }
    }
}