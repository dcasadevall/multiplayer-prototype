using System;
using System.Numerics;
using System.Text.Json;
using Shared.ECS.Components;
using Shared.Input;
using Xunit;

namespace SharedUnitTests.ECS.Components
{
    /// <summary>
    /// Tests containing all components used by a player shot
    /// </summary>
    public class PlayerShotComponentTests
    {
        [Fact]
        public void DamageApplyingComponent_ShouldSerializeCorrectly()
        {
            // Arrange
            var component = new DamageApplyingComponent
            {
                Damage = 25,
                CanDamageSelf = false
            };

            // Act
            var json = JsonSerializer.Serialize(component);
            var deserialized = JsonSerializer.Deserialize<DamageApplyingComponent>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(25, deserialized.Damage);
            Assert.False(deserialized.CanDamageSelf);
        }

        [Fact]
        public void SpawnAuthorityComponent_ShouldSerializeCorrectly()
        {
            // Arrange
            var localId = Guid.NewGuid();
            var component = new SpawnAuthorityComponent
            {
                SpawnedByPeerId = 42,
                LocalEntityId = localId,
                SpawnTick = 100
            };

            // Act
            var json = JsonSerializer.Serialize(component);
            var deserialized = JsonSerializer.Deserialize<SpawnAuthorityComponent>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(42, deserialized.SpawnedByPeerId);
            Assert.Equal(localId, deserialized.LocalEntityId);
            Assert.Equal(100u, deserialized.SpawnTick);
        }

        [Fact]
        public void PlayerShotMessage_ShouldSerializeCorrectly()
        {
            // Arrange
            var predictedId = Guid.NewGuid();
            var message = new PlayerShotMessage
            {
                Tick = 150,
                PredictedProjectileId = predictedId
            };

            // Act
            var json = JsonSerializer.Serialize(message);
            var deserialized = JsonSerializer.Deserialize<PlayerShotMessage>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(150u, deserialized.Tick);
            Assert.Equal(predictedId, deserialized.PredictedProjectileId);
        }

        [Fact]
        public void SelfDestroyingComponent_CreateWithTTL_ShouldCalculateCorrectDestroyTick()
        {
            // Arrange
            uint currentTick = 100;
            // 2 seconds at 30 FPS
            uint ttl = 60;

            // Act
            var component = SelfDestroyingComponent.CreateWithTTL(currentTick, ttl);

            // Assert
            Assert.Equal(160u, component.DestroyAtTick);
            Assert.False(component.IsMarkedForDestruction);
        }

        [Fact]
        public void ProjectileTagComponent_ShouldBeEmptyTagComponent()
        {
            // Arrange & Act
            var component = new ProjectileTagComponent();

            // Assert
            Assert.NotNull(component);
            // Just verify it exists as a tag component - no properties to test
        }
    }
}