using System.Numerics;
using Shared;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using Shared.Respawn;
using Xunit;

namespace SharedUnitTests.Damage
{
    public class DeathSystemTests
    {
        [Fact]
        public void Update_CreatesDeathRecordWhenEntityDies()
        {
            var registry = new EntityRegistry();
            var system = new DeathSystem();

            var player = PlayerArchetype.Create(registry, 1, Vector3.Zero);
            player.AddOrReplaceComponent(new HealthComponent { CurrentHealth = 0 });

            system.Update(registry, 42, 0.016f);

            Assert.False(registry.TryGet(player.Id, out _));

            var deathRecords = registry.With<RespawnComponent>().ToList();
            Assert.Single(deathRecords);
            Assert.True(deathRecords[0].Has<PlayerTagComponent>());
        }

        [Fact]
        public void Update_AddsRespawnAtTickToDeadEntity()
        {
            var registry = new EntityRegistry();
            var system = new DeathSystem();

            var player = PlayerArchetype.Create(registry, 1, Vector3.Zero);
            player.AddOrReplaceComponent(new HealthComponent { CurrentHealth = 0 });

            system.Update(registry, 42, 0.016f);

            var respawnable = player.GetRequired<RespawnComponent>();
            Assert.Equal(42 + GameplayConstants.PlayerRespawnTime.ToNumTicks(), respawnable.RespawnAtTick);
            Assert.False(player.Has<HealthComponent>());
        }

        [Fact]
        public void Update_DoesNotAddRespawnAtTickTwice()
        {
            var registry = new EntityRegistry();
            var system = new DeathSystem();

            var player = PlayerArchetype.Create(registry, 1, Vector3.Zero);
            player.AddOrReplaceComponent(new HealthComponent { CurrentHealth = 0 });

            system.Update(registry, 1, 0.016f);
            var firstRespawnTick = player.GetRequired<RespawnComponent>().RespawnAtTick;
            system.Update(registry, 2, 0.016f);
            var secondRespawnTick = player.GetRequired<RespawnComponent>().RespawnAtTick;

            Assert.Equal(firstRespawnTick, secondRespawnTick);
        }
    }
}