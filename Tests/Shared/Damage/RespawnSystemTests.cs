using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Respawn;
using Xunit;

namespace SharedUnitTests.Damage
{
    /// <summary>
    /// Unit tests for <see cref="RespawnSystem"/>.
    /// 
    /// Test structure follows the Arrange-Act-Assert pattern for clarity and maintainability.
    /// See: https://medium.com/@kaanfurkanc/unit-testing-best-practices-3a8b0ddd88b5
    /// </summary>
    public class RespawnSystemTests
    {
        [Fact]
        public void Update_RespawnsEntityAfterRespawnTime()
        {
            // Arrange: Setup registry, system, and a death record entity with respawn time
            var registry = new EntityRegistry();
            var system = new RespawnSystem();

            var deathRecord = registry.CreateEntity();
            deathRecord.AddComponent(new RespawnComponent { RespawnAtTick = 10 });
            deathRecord.AddComponent(new PlayerTagComponent());
            deathRecord.AddComponent(new PeerComponent { PeerId = 1 });

            // Act: Run the respawn system update at the respawn tick
            system.Update(registry, 10, 0.016f);

            // Assert: Death record should be removed, new player entity should be created with correct PeerId
            Assert.False(registry.TryGet(deathRecord.Id, out _)); // Ensure death record is removed
            var players = registry.With<PlayerTagComponent>().ToList();
            Assert.Single(players);

            var newPlayer = players.First();
            Assert.NotEqual(deathRecord.Id, newPlayer.Id);
            Assert.True(newPlayer.Has<PeerComponent>());
            Assert.Equal(1, newPlayer.Get<PeerComponent>()!.PeerId);
        }
    }
}