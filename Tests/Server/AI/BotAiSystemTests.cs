using System.Numerics;
using Server.AI;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Physics;
using Xunit;

namespace ServerUnitTests.AI
{
    public class BotAiSystemTests
    {
        private readonly EntityRegistry _registry;
        private readonly BotAiSystem _system;
        private readonly Entity _player;
        private readonly Entity _bot;

        public BotAiSystemTests()
        {
            _registry = new EntityRegistry();
            _system = new BotAiSystem();

            // Create a mock player
            _player = _registry.CreateEntity();
            _player.AddComponent(new PlayerTagComponent());
            _player.AddComponent(new PositionComponent { Value = Vector3.Zero });

            // Create a mock bot
            _bot = _registry.CreateEntity();
            _bot.AddComponent(new BotTagComponent());
            _bot.AddComponent(new PositionComponent { Value = new Vector3(10, 0, 10) });
            _bot.AddComponent(new HealthComponent { CurrentHealth = 100, MaxHealth = 100 });
        }

        [Fact]
        public void Update_WhenBotHealthIsLow_RetreatsFromPlayer()
        {
            // Arrange
            _bot.GetRequired<HealthComponent>().CurrentHealth = 10; // Low health

            // Act
            _system.Update(_registry, 0, 0);

            // Assert
            var velocity = _bot.GetRequired<VelocityComponent>().Value;
            var directionToPlayer =
                Vector3.Normalize(_player.GetRequired<PositionComponent>().Value - _bot.GetRequired<PositionComponent>().Value);
            Assert.True(Vector3.Dot(Vector3.Normalize(velocity), directionToPlayer) < 0, "Bot should move away from the player.");
        }

        [Fact]
        public void Update_WhenBotHasNoTarget_RoamsRandomly()
        {
            // Arrange
            _registry.DestroyEntity(_player.Id); // No players

            // Act
            _system.Update(_registry, 0, 0);

            // Assert
            Assert.True(_bot.Has<RoamingStateComponent>());
            Assert.NotEqual(Vector3.Zero, _bot.GetRequired<VelocityComponent>().Value);
        }

        [Fact]
        public void Update_WhenBotHasTargetAndIsOutOfRange_MovesTowardsTarget()
        {
            // Arrange
            _bot.AddOrReplaceComponent(new TargetComponent { TargetId = _player.Id.Value });
            _bot.GetRequired<PositionComponent>().Value = new Vector3(20, 0, 20); // Far from player

            // Act
            _system.Update(_registry, 0, 0);

            // Assert
            var velocity = _bot.GetRequired<VelocityComponent>().Value;
            var directionToPlayer =
                Vector3.Normalize(_player.GetRequired<PositionComponent>().Value - _bot.GetRequired<PositionComponent>().Value);
            Assert.True(Vector3.Dot(Vector3.Normalize(velocity), directionToPlayer) > 0.9f, "Bot should move towards the player.");
        }

        [Fact]
        public void Update_WhenBotHasTargetAndIsInRange_AttacksTarget()
        {
            // Arrange
            _bot.AddOrReplaceComponent(new TargetComponent { TargetId = _player.Id.Value });
            _bot.GetRequired<PositionComponent>().Value = new Vector3(1, 0, 1); // Close to player

            // Act
            _system.Update(_registry, 0, 0);

            // Assert
            Assert.Equal(Vector3.Zero, _bot.GetRequired<VelocityComponent>().Value);
            Assert.True(_bot.Has<ShootingCooldownComponent>());
        }

        [Fact]
        public void Update_WhenRoamingAndPlayerAppears_AcquiresTarget()
        {
            // Arrange
            _registry.DestroyEntity(_player.Id); // No players initially
            _system.Update(_registry, 0, 0); // Start roaming
            var newPlayer = _registry.CreateEntity();
            newPlayer.AddComponent(new PlayerTagComponent());
            newPlayer.AddComponent(new PositionComponent { Value = new Vector3(5, 0, 5) });

            // Act
            _system.Update(_registry, 1, 0);

            // Assert
            Assert.True(_bot.Has<TargetComponent>());
            Assert.Equal(newPlayer.Id.Value, _bot.GetRequired<TargetComponent>().TargetId);
            Assert.False(_bot.Has<RoamingStateComponent>());
        }
    }
}