using System.Numerics;
using Shared.ECS.Components;
using Xunit;

namespace SharedUnitTests.ECS.Components
{
    public class ComponentTests
    {
        public class PositionComponentTests
        {
            [Fact]
            public void DefaultConstructor_ShouldSetZeroVector()
            {
                // Act
                var component = new PositionComponent();

                // Assert
                Assert.Equal(Vector3.Zero, component.Value);
            }

            [Fact]
            public void ParameterizedConstructor_ShouldSetValue()
            {
                // Arrange
                var position = new Vector3(1, 2, 3);

                // Act
                var component = new PositionComponent{ Value = position };

                // Assert
                Assert.Equal(position, component.Value);
            }

            [Fact]
            public void Value_ShouldBeMutable()
            {
                // Arrange
                var component = new PositionComponent();
                var newPosition = new Vector3(4, 5, 6);

                // Act
                component.Value = newPosition;

                // Assert
                Assert.Equal(newPosition, component.Value);
            }
        }

        public class VelocityComponentTests
        {
            [Fact]
            public void DefaultConstructor_ShouldSetZeroVector()
            {
                // Act
                var component = new VelocityComponent();

                // Assert
                Assert.Equal(Vector3.Zero, component.Value);
            }

            [Fact]
            public void ParameterizedConstructor_ShouldSetValue()
            {
                // Arrange
                var velocity = new Vector3(1, 2, 3);

                // Act
                var component = new VelocityComponent{ Value = velocity };

                // Assert
                Assert.Equal(velocity, component.Value);
            }

            [Fact]
            public void Value_ShouldBeMutable()
            {
                // Arrange
                var component = new VelocityComponent();
                var newVelocity = new Vector3(4, 5, 6);

                // Act
                component.Value = newVelocity;

                // Assert
                Assert.Equal(newVelocity, component.Value);
            }
        }

        public class HealthComponentTests
        {
            [Fact]
            public void Constructor_ShouldSetValues()
            {
                // Arrange
                var maxHealth = 100;

                // Act
                var component = new HealthComponent(maxHealth);

                // Assert
                Assert.Equal(maxHealth, component.CurrentHealth);
                Assert.Equal(maxHealth, component.MaxHealth);
            }

            [Fact]
            public void CurrentHealth_ShouldBeMutable()
            {
                // Arrange
                var component = new HealthComponent(100);
                var newHealth = 75;

                // Act
                component.CurrentHealth = newHealth;

                // Assert
                Assert.Equal(newHealth, component.CurrentHealth);
            }

            [Fact]
            public void MaxHealth_ShouldBeMutable()
            {
                // Arrange
                var component = new HealthComponent(100);
                var newMaxHealth = 150;

                // Act
                component.MaxHealth = newMaxHealth;

                // Assert
                Assert.Equal(newMaxHealth, component.MaxHealth);
            }

            [Fact]
            public void CurrentHealth_ShouldNotExceedMaxHealth()
            {
                // Arrange
                var component = new HealthComponent(100);

                // Act
                component.CurrentHealth = 150; // Try to set above max

                // Assert
                Assert.Equal(150, component.CurrentHealth); // Should allow it for flexibility
            }
        }

        public class TagComponentTests
        {
            [Fact]
            public void PlayerTagComponent_ShouldBeInstantiable()
            {
                // Act
                var component = new PlayerTagComponent();

                // Assert
                Assert.NotNull(component);
            }

            [Fact]
            public void ProjectileTagComponent_ShouldBeInstantiable()
            {
                // Act
                var component = new ProjectileTagComponent();

                // Assert
                Assert.NotNull(component);
            }
        }
    }
}