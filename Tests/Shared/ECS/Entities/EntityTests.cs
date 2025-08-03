using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Xunit;

namespace SharedUnitTests.ECS.Entities
{
    public class EntityTests
    {
        [Fact]
        public void Constructor_ShouldSetId()
        {
            // Arrange
            var id = EntityId.New();

            // Act
            var entity = new Entity(id);

            // Assert
            Assert.Equal(id, entity.Id);
        }

        [Fact]
        public void AddComponent_ShouldStoreComponent()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position = new PositionComponent{ Value = new Vector3(1, 2, 3)};

            // Act
            entity.AddComponent(position);

            // Assert
            Assert.True(entity.Has<PositionComponent>());
        }

        [Fact]
        public void TryGet_WithExistingComponent_ShouldReturnTrueAndComponent()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position = new PositionComponent{ Value = new Vector3(1, 2, 3) };
            entity.AddComponent(position);

            // Act
            var success = entity.TryGet<PositionComponent>(out var retrievedPosition);

            // Assert
            Assert.True(success);
            Assert.NotNull(retrievedPosition);
            Assert.Equal(position.Value, retrievedPosition.Value);
        }

        [Fact]
        public void TryGet_WithNonExistentComponent_ShouldReturnFalseAndNull()
        {
            // Arrange
            var entity = new Entity(EntityId.New());

            // Act
            var success = entity.TryGet<PositionComponent>(out var retrievedPosition);

            // Assert
            Assert.False(success);
            Assert.Null(retrievedPosition);
        }

        [Fact]
        public void Has_WithExistingComponent_ShouldReturnTrue()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position = new PositionComponent();
            entity.AddComponent(position);

            // Act
            var hasComponent = entity.Has<PositionComponent>();

            // Assert
            Assert.True(hasComponent);
        }

        [Fact]
        public void Has_WithNonExistentComponent_ShouldReturnFalse()
        {
            // Arrange
            var entity = new Entity(EntityId.New());

            // Act
            var hasComponent = entity.Has<PositionComponent>();

            // Assert
            Assert.False(hasComponent);
        }

        [Fact]
        public void Remove_WithExistingComponent_ShouldRemoveComponent()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position = new PositionComponent();
            entity.AddComponent(position);

            // Act
            entity.Remove<PositionComponent>();

            // Assert
            Assert.False(entity.Has<PositionComponent>());
        }

        [Fact]
        public void Remove_WithNonExistentComponent_ShouldNotThrow()
        {
            // Arrange
            var entity = new Entity(EntityId.New());

            // Act & Assert
            var exception = Record.Exception(() => entity.Remove<PositionComponent>());
            Assert.Null(exception);
        }

        [Fact]
        public void GetAllComponents_ShouldReturnAllComponents()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position = new PositionComponent
            {
                Value = new Vector3(1, 2, 3)
            };
            var velocity = new VelocityComponent { Value = new Vector3(4, 5, 6) };
            var health = new HealthComponent(100);

            entity.AddComponent(position);
            entity.AddComponent(velocity);
            entity.AddComponent(health);

            // Act
            var components = entity.GetAllComponents().ToList();

            // Assert
            Assert.Equal(3, components.Count);
            Assert.Contains(position, components);
            Assert.Contains(velocity, components);
            Assert.Contains(health, components);
        }

        [Fact]
        public void GetAllComponents_WithNoComponents_ShouldReturnEmptyEnumerable()
        {
            // Arrange
            var entity = new Entity(EntityId.New());

            // Act
            var components = entity.GetAllComponents().ToList();

            // Assert
            Assert.Empty(components);
        }

        [Fact]
        public void AddComponent_WithSameType_ShouldReplaceExistingComponent()
        {
            // Arrange
            var entity = new Entity(EntityId.New());
            var position1 = new PositionComponent{ Value = new Vector3(1, 2, 3)};
            var position2 = new PositionComponent{ Value = new Vector3(4, 5, 6)};

            entity.AddComponent(position1);

            // Act
            entity.AddComponent(position2);

            // Assert
            Assert.True(entity.TryGet<PositionComponent>(out var retrievedPosition));
            Assert.Equal(position2.Value, retrievedPosition.Value);
        }
    }
}