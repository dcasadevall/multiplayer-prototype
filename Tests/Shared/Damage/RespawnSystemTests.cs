using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
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

            Assert.True(deathRecord.Has<MarkedForRemovalTagComponent>());
            var players = registry.With<PlayerTagComponent>().ToList();
            Assert.Single(players);

            var newPlayer = players.First();
            Assert.NotEqual(deathRecord.Id, newPlayer.Id);
            Assert.True(newPlayer.Has<PeerComponent>());
            Assert.Equal(1, newPlayer.Get<PeerComponent>().PeerId);
        }
    }
}