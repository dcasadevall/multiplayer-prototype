using Shared;
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
            Assert.True(GameplayConstants.ProjectileTtl.TotalSeconds > 0, "Laser TTL should be positive");
            Assert.True(GameplayConstants.ProjectileDamage > 0, "Laser damage should be positive");
        }
    }
}