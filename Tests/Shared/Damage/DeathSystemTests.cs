using System.Numerics;
using Shared.Damage;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Respawn;
using Shared.Settings;
using Xunit;

namespace SharedUnitTests.Damage
{
    /// <summary>
    /// Unit tests for <see cref="DeathSystem"/>.
    /// 
    /// Test structure follows the Arrange-Act-Assert pattern for clarity and maintainability.
    /// See: https://medium.com/@kaanfurkanc/unit-testing-best-practices-3a8b0ddd88b5
    /// </summary>
    public class DeathSystemTests
    {
        [Fact]
        public void Update_CreatesDeathRecordWhenEntityDies()
        {
            // Arrange: Setup registry, system, and a player with zero health
            var registry = new EntityRegistry();
            var playerSettings = new PlayerSettings();
            var botSettings = new BotSettings();
            var simulationSettings = new SimulationSettings();
            var playerFactory = new PlayerFactory(registry, playerSettings);
            var system = new DeathSystem(playerSettings, botSettings, simulationSettings);
            var player = playerFactory.Create(1, Vector3.Zero);
            player.AddOrReplaceComponent(new HealthComponent
            {
                CurrentHealth = 0,
                MaxHealth = 100,
            });

            // Act: Run the death system update
            system.Update(registry, 42, 0.016f);

            // Assert: Player entity should be removed, and a death record created with PeerComponent
            Assert.False(registry.TryGet(player.Id, out _));
            var deathRecords = registry.With<RespawnComponent>().ToList();
            Assert.Single(deathRecords);
            Assert.True(deathRecords[0].Has<PeerComponent>());
        }

        [Fact]
        public void Update_AddsRespawnAtTickToDeadEntity()
        {
            // Arrange: Setup registry, system, and a player with zero health
            var registry = new EntityRegistry();
            var playerSettings = new PlayerSettings();
            var playerFactory = new PlayerFactory(registry, playerSettings);
            var player = playerFactory.Create(1, Vector3.Zero);
            var botSettings = new BotSettings();
            var simulationSettings = new SimulationSettings();
            var system = new DeathSystem(playerSettings, botSettings, simulationSettings);
            player.AddOrReplaceComponent(new HealthComponent
            {
                CurrentHealth = 0,
                MaxHealth = 100,
            });

            // Act: Run the death system update
            system.Update(registry, 42, 0.016f);

            // Assert: Death record should have correct RespawnAtTick value
            var deathRecord = registry.With<RespawnComponent>().Single();
            var respawnable = deathRecord.GetRequired<RespawnComponent>();
            Assert.Equal(
                42 + playerSettings.PlayerRespawnTime.ToNumTicks(simulationSettings.WorldTicksPerSecond),
                respawnable.RespawnAtTick);
        }

        [Fact]
        public void Update_DoesNotCreateDeathRecordTwice()
        {
            // Arrange: Setup registry, system, and a player with zero health
            var registry = new EntityRegistry();
            var playerSettings = new PlayerSettings();
            var playerFactory = new PlayerFactory(registry, playerSettings);
            var botSettings = new BotSettings();
            var simulationSettings = new SimulationSettings();
            var system = new DeathSystem(playerSettings, botSettings, simulationSettings);
            var player = playerFactory.Create(1, Vector3.Zero);

            player.AddOrReplaceComponent(new HealthComponent
            {
                CurrentHealth = 0,
                MaxHealth = 100,
            });

            // Act: Run the death system update twice
            system.Update(registry, 1, 0.016f);
            system.Update(registry, 2, 0.016f);

            // Assert: Only one death record should be created
            var deathRecords = registry.With<RespawnComponent>().ToList();
            Assert.Single(deathRecords);
        }

        [Fact]
        public void Update_WhenEntityIsKilled_DeathSystemCreatesDeathRecord()
        {
            // Arrange
            var registry = new EntityRegistry();
            var playerSettings = new PlayerSettings();
            var playerFactory = new PlayerFactory(registry, playerSettings);
            var botSettings = new BotSettings();
            var simulationSettings = new SimulationSettings();
            var system = new DeathSystem(playerSettings, botSettings, simulationSettings);
            var player = playerFactory.Create(1, Vector3.Zero);

            // Act
            // Simulate the player taking lethal damage
            var health = player.GetRequired<HealthComponent>();
            var newHealth = new HealthComponent { MaxHealth = health.MaxHealth, CurrentHealth = 0 };
            player.AddOrReplaceComponent(newHealth);

            system.Update(registry, 1, 0.016f);

            // Assert
            var deathRecords = registry.With<RespawnComponent>().ToList();
            Assert.Single(deathRecords);
            Assert.True(deathRecords[0].Has<PeerComponent>());
        }
    }
}