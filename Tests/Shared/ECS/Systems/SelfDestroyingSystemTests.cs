using System.Linq;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Systems;
using Shared.Logging;
using Xunit;

namespace SharedUnitTests.ECS.Systems
{
    public class SelfDestroyingSystemTests
    {
        [Fact]
        public void Update_EntityWithExpiredTTL_ShouldDestroyEntity()
        {
            // Arrange
            var registry = new EntityRegistry();
            var logger = Substitute.For<ILogger>();
            var system = new SelfDestroyingSystem(logger);

            var entity = registry.CreateEntity();
            entity.AddComponent(new SelfDestroyingComponent { DestroyAtTick = 10 });

            // Act - run at tick 15 (past expiration)
            system.Update(registry, 15, 0.033f);

            // Assert
            Assert.False(registry.TryGet(entity.Id, out _));
        }

        [Fact]
        public void Update_EntityWithUnexpiredTTL_ShouldNotDestroyEntity()
        {
            // Arrange
            var registry = new EntityRegistry();
            var logger = Substitute.For<ILogger>();
            var system = new SelfDestroyingSystem(logger);

            var entity = registry.CreateEntity();
            entity.AddComponent(new SelfDestroyingComponent { DestroyAtTick = 20 });

            // Act - run at tick 15 (before expiration)
            system.Update(registry, 15, 0.033f);

            // Assert
            Assert.True(registry.TryGet(entity.Id, out _));
        }

        [Fact]
        public void Update_MultipleEntitiesWithDifferentTTLs_ShouldDestroyOnlyExpiredOnes()
        {
            // Arrange
            var registry = new EntityRegistry();
            var logger = Substitute.For<ILogger>();
            var system = new SelfDestroyingSystem(logger);

            var expiredEntity = registry.CreateEntity();
            expiredEntity.AddComponent(new SelfDestroyingComponent { DestroyAtTick = 10 });

            var activeEntity = registry.CreateEntity();
            activeEntity.AddComponent(new SelfDestroyingComponent { DestroyAtTick = 25 });

            // Act - run at tick 15
            system.Update(registry, 15, 0.033f);

            // Assert
            Assert.False(registry.TryGet(expiredEntity.Id, out _));
            Assert.True(registry.TryGet(activeEntity.Id, out _));
        }

        [Fact]
        public void CreateWithTTL_ShouldSetCorrectDestroyAtTick()
        {
            // Arrange
            uint currentTick = 100;
            uint ttl = 30;

            // Act
            var component = SelfDestroyingComponent.CreateWithTTL(currentTick, ttl);

            // Assert
            Assert.Equal(130u, component.DestroyAtTick);
        }
    }
}