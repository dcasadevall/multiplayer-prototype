using Shared.Input;
using Xunit;

namespace SharedUnitTests.Input
{
    public class GameplayConstantsTests
    {
        [Fact]
        public void LaserConstants_ShouldHaveReasonableValues()
        {
            // Arrange & Act & Assert
            Assert.True(GameplayConstants.ProjectileSpeed > 0, "Laser speed should be positive");
            Assert.True(GameplayConstants.ProjectileTtlTicks > 0, "Laser TTL should be positive");
            Assert.True(GameplayConstants.ProjectileDamage > 0, "Laser damage should be positive");
            Assert.True(GameplayConstants.PlayerShotCooldownTicks > 0, "Laser cooldown should be positive");

            // Verify cooldown is reasonable (not too fast, not too slow)
            Assert.True(GameplayConstants.PlayerShotCooldownTicks >= 5, "Cooldown should be at least 5 ticks (0.16s at 30fps)");
            Assert.True(GameplayConstants.PlayerShotCooldownTicks <= 90, "Cooldown should be at most 90 ticks (3s at 30fps)");

            // Verify TTL is reasonable (projectiles don't live forever but not too short)
            Assert.True(GameplayConstants.ProjectileTtlTicks >= 30, "TTL should be at least 30 ticks (1s at 30fps)");
            Assert.True(GameplayConstants.ProjectileTtlTicks <= 300, "TTL should be at most 300 ticks (10s at 30fps)");
        }

        [Fact]
        public void LaserCooldown_ShouldAllowReasonableFireRate()
        {
            // Arrange
            const uint ticksPerSecond = 30;
            var cooldownSeconds = GameplayConstants.PlayerShotCooldownTicks / (float)ticksPerSecond;
            var maxFireRate = 1f / cooldownSeconds; // shots per second

            // Act & Assert
            Assert.True(maxFireRate >= 1f, "Should allow at least 1 shot per second");
            Assert.True(maxFireRate <= 10f, "Should not allow more than 10 shots per second");
        }
    }
}