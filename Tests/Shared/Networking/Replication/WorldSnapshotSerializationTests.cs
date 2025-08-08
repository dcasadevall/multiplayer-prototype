using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Health;
using Shared.Logging;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.Networking.Replication
{
    public class WorldSnapshotSerializationTests
    {
        [Fact]
        public void ProduceAndConsume_WithMultipleEntities_WorksCorrectly()
        {
            // Arrange - Create source registry with some entities
            var sourceRegistry = new EntityRegistry();
            var targetRegistry = new EntityRegistry();
            var sourceEntities = CreateTestEntities(sourceRegistry);

            // Create producer and consumer
            var logger = Substitute.For<ILogger>();
            var producer = new JsonWorldSnapshotProducer(sourceRegistry, logger);
            var consumer = new JsonWorldSnapshotConsumer(targetRegistry, logger);

            // Act - Produce snapshot from source and consume it in target
            var snapshot = producer.ProduceSnapshot();
            consumer.ConsumeSnapshot(snapshot);

            // Assert - Verify entities were recreated correctly
            var targetEntities = targetRegistry.GetAll();
            Assert.Equal(sourceEntities.Length, targetEntities.ToList().Count);

            // Verify each source entity has a matching target entity
            foreach (var sourceEntity in sourceEntities)
            {
                var targetEntity = targetRegistry.GetOrCreate(sourceEntity.Id.Value);

                // Check position component
                if (sourceEntity.Has<PositionComponent>())
                {
                    Assert.True(targetEntity.Has<PositionComponent>());
                    var sourcePos = sourceEntity.Get<PositionComponent>();
                    var targetPos = targetEntity.Get<PositionComponent>();
                    Assert.NotNull(sourcePos);
                    Assert.NotNull(targetPos);
                    Assert.Equal(sourcePos.X, targetPos.X);
                    Assert.Equal(sourcePos.Y, targetPos.Y);
                    Assert.Equal(sourcePos.Z, targetPos.Z);
                }

                // Check velocity component
                if (sourceEntity.Has<VelocityComponent>())
                {
                    Assert.True(targetEntity.Has<VelocityComponent>());
                    var sourceVel = sourceEntity.Get<VelocityComponent>();
                    var targetVel = targetEntity.Get<VelocityComponent>();
                    Assert.NotNull(sourceVel);
                    Assert.NotNull(targetVel);
                    Assert.Equal(sourceVel.X, targetVel.X);
                    Assert.Equal(sourceVel.Y, targetVel.Y);
                    Assert.Equal(sourceVel.Z, targetVel.Z);
                }

                // Check health component
                if (sourceEntity.Has<HealthComponent>())
                {
                    Assert.True(targetEntity.Has<HealthComponent>());
                    var sourceHealth = sourceEntity.Get<HealthComponent>();
                    var targetHealth = targetEntity.Get<HealthComponent>();
                    Assert.NotNull(sourceHealth);
                    Assert.NotNull(targetHealth);
                    Assert.Equal(sourceHealth.MaxHealth, targetHealth.MaxHealth);
                    Assert.Equal(sourceHealth.CurrentHealth, targetHealth.CurrentHealth);
                }
            }
        }

        [Fact]
        public void ProduceAndConsume_WithEmptySnapshot_DoesNothing()
        {
            // Arrange
            var sourceRegistry = new EntityRegistry();
            var targetRegistry = new EntityRegistry();
            var logger = Substitute.For<ILogger>();
            var producer = new JsonWorldSnapshotProducer(sourceRegistry, logger);
            var consumer = new JsonWorldSnapshotConsumer(targetRegistry, logger);

            // Act
            var snapshot = producer.ProduceSnapshot();
            consumer.ConsumeSnapshot(snapshot);

            // Assert
            Assert.Empty(targetRegistry.GetAll());
        }

        [Fact]
        public void ProduceAndConsume_OnlyReplicatesTaggedEntities()
        {
            // Arrange
            var sourceRegistry = new EntityRegistry();
            var targetRegistry = new EntityRegistry();

            // Create one replicated and one non-replicated entity
            var replicatedEntity = sourceRegistry.CreateEntity();
            replicatedEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            replicatedEntity.AddComponent(new ReplicatedTagComponent());

            var nonReplicatedEntity = sourceRegistry.CreateEntity();
            nonReplicatedEntity.AddComponent(new PositionComponent { X = 4, Y = 5, Z = 6 });

            var logger = Substitute.For<ILogger>();
            var producer = new JsonWorldSnapshotProducer(sourceRegistry, logger);
            var consumer = new JsonWorldSnapshotConsumer(targetRegistry, logger);

            // Act
            var snapshot = producer.ProduceSnapshot();
            consumer.ConsumeSnapshot(snapshot);

            // Assert
            var targetEntities = targetRegistry.GetAll();
            Assert.Single(targetEntities); // Only the replicated entity should be present

            var targetEntity = targetEntities.First();
            Assert.True(targetEntity.Has<PositionComponent>());
            var targetPos = targetEntity.Get<PositionComponent>();
            Assert.NotNull(targetPos);
            Assert.Equal(1, targetPos.X);
            Assert.Equal(2, targetPos.Y);
            Assert.Equal(3, targetPos.Z);
        }

        private Entity[] CreateTestEntities(EntityRegistry registry)
        {
            // Create a stationary entity
            var entity1 = registry.CreateEntity();
            entity1.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            entity1.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 75 });
            entity1.AddComponent(new ReplicatedTagComponent());

            // Create a moving entity
            var entity2 = registry.CreateEntity();
            entity2.AddComponent(new PositionComponent { X = 4, Y = 5, Z = 6 });
            entity2.AddComponent(new VelocityComponent { X = 1, Y = 0, Z = 1 });
            entity2.AddComponent(new HealthComponent { MaxHealth = 150, CurrentHealth = 150 });
            entity2.AddComponent(new ReplicatedTagComponent());

            // Create an entity with just position
            var entity3 = registry.CreateEntity();
            entity3.AddComponent(new PositionComponent { X = 7, Y = 8, Z = 9 });
            entity3.AddComponent(new ReplicatedTagComponent());

            return new[] { entity1, entity2, entity3 };
        }
    }
}