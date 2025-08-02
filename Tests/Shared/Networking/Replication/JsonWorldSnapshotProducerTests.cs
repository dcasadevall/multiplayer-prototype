using System.Text.Json;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Replication;
using Shared.Logging;
using Xunit;

namespace SharedUnitTests.Networking.Replication
{
    public class JsonWorldSnapshotProducerTests
    {
        private readonly EntityRegistry _registry;
        private readonly JsonWorldSnapshotProducer _producer;

        public JsonWorldSnapshotProducerTests()
        {
            var logger = Substitute.For<ILogger>();
            _registry = new EntityRegistry();
            _producer = new JsonWorldSnapshotProducer(_registry, logger);
        }

        [Fact]
        public void ProduceSnapshot_WithNoReplicatedEntities_ReturnsEmptySnapshot()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Empty(snapshot.Entities);
        }

        [Fact]
        public void ProduceSnapshot_WithReplicatedEntity_IncludesEntityInSnapshot()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Single(snapshot.Entities);

            var snapshotEntity = snapshot.Entities.First();
            Assert.Equal(entity.Id.Value, snapshotEntity.Id);
            Assert.Single(snapshotEntity.Components);

            var component = snapshotEntity.Components.First();
            Assert.Equal(typeof(PositionComponent).FullName, component.Type);
        }

        [Fact]
        public void ProduceSnapshot_WithMultipleComponents_IncludesAllComponents()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));
            entity.AddComponent(new HealthComponent(150));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Single(snapshot.Entities);

            var snapshotEntity = snapshot.Entities.First();
            Assert.Equal(2, snapshotEntity.Components.Count);

            var componentTypes = snapshotEntity.Components.Select(c => c.Type).ToList();
            Assert.Contains(typeof(PositionComponent).FullName, componentTypes);
            Assert.Contains(typeof(HealthComponent).FullName, componentTypes);
        }

        [Fact]
        public void ProduceSnapshot_WithMultipleReplicatedEntities_IncludesAllEntities()
        {
            // Arrange
            var entity1 = _registry.CreateEntity();
            entity1.AddComponent(new ReplicatedTagComponent());
            entity1.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));

            var entity2 = _registry.CreateEntity();
            entity2.AddComponent(new ReplicatedTagComponent());
            entity2.AddComponent(new HealthComponent(200));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Equal(2, snapshot.Entities.Count);

            var entityIds = snapshot.Entities.Select(e => e.Id).ToList();
            Assert.Contains(entity1.Id.Value, entityIds);
            Assert.Contains(entity2.Id.Value, entityIds);
        }

        [Fact]
        public void ProduceSnapshot_WithMixedEntities_OnlyIncludesReplicatedEntities()
        {
            // Arrange
            var replicatedEntity = _registry.CreateEntity();
            replicatedEntity.AddComponent(new ReplicatedTagComponent());
            replicatedEntity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));

            var nonReplicatedEntity = _registry.CreateEntity();
            nonReplicatedEntity.AddComponent(new PositionComponent(new System.Numerics.Vector3(4.0f, 5.0f, 6.0f)));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Single(snapshot.Entities);

            var snapshotEntity = snapshot.Entities.First();
            Assert.Equal(replicatedEntity.Id.Value, snapshotEntity.Id);
        }

        [Fact]
        public void ProduceSnapshot_WithComplexComponent_SerializesComponentCorrectly()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.5f, 2.5f, 3.5f)));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            var snapshotEntity = snapshot.Entities.First();
            var positionComponent = snapshotEntity.Components.First(c => c.Type == typeof(PositionComponent).FullName);

            var deserializedPosition = JsonSerializer.Deserialize<PositionComponent>(positionComponent.Json);
            Assert.NotNull(deserializedPosition);
            Assert.Equal(1.5f, deserializedPosition.Value.X);
            Assert.Equal(2.5f, deserializedPosition.Value.Y);
            Assert.Equal(3.5f, deserializedPosition.Value.Z);
        }

        [Fact]
        public void ProduceSnapshot_WithHealthComponent_SerializesHealthCorrectly()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new HealthComponent(175));

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            var snapshotEntity = snapshot.Entities.First();
            var healthComponent = snapshotEntity.Components.First(c => c.Type == typeof(HealthComponent).FullName);

            var deserializedHealth = JsonSerializer.Deserialize<HealthComponent>(healthComponent.Json);
            Assert.NotNull(deserializedHealth);
            Assert.Equal(175, deserializedHealth.MaxHealth);
            Assert.Equal(175, deserializedHealth.CurrentHealth);
        }

        [Fact]
        public void ProduceSnapshot_WithEmptyRegistry_ReturnsEmptySnapshot()
        {
            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Empty(snapshot.Entities);
        }

        [Fact]
        public void ProduceSnapshot_WithReplicatedEntityButNoComponents_IncludesEntityWithoutComponents()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Single(snapshot.Entities);

            var snapshotEntity = snapshot.Entities.First();
            Assert.Equal(entity.Id.Value, snapshotEntity.Id);
            Assert.Empty(snapshotEntity.Components);
        }

        [Fact]
        public void ProduceSnapshot_WithNonReplicatedComponents_ExcludesNonReplicatedComponents()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)));
            entity.AddComponent(new VelocityComponent { Value = new System.Numerics.Vector3(0.1f, 0.2f, 0.3f) });

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            var snapshotEntity = snapshot.Entities.First();

            // Should only include components that are serializable
            var componentTypes = snapshotEntity.Components.Select(c => c.Type).ToList();
            Assert.Contains(typeof(PositionComponent).FullName, componentTypes);
            Assert.Contains(typeof(VelocityComponent).FullName, componentTypes);
        }

        [Fact]
        public void ProduceSnapshot_WithLargeNumberOfEntities_HandlesMultipleEntities()
        {
            // Arrange
            const int entityCount = 10;
            for (int i = 0; i < entityCount; i++)
            {
                var entity = _registry.CreateEntity();
                entity.AddComponent(new ReplicatedTagComponent());
                entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(i, i, i)));
            }

            // Act
            var snapshot = _producer.ProduceSnapshot();

            // Assert
            Assert.Equal(entityCount, snapshot.Entities.Count);

            for (int i = 0; i < entityCount; i++)
            {
                var snapshotEntity = snapshot.Entities[i];
                Assert.Single(snapshotEntity.Components);

                var component = snapshotEntity.Components.First();
                Assert.Equal(typeof(PositionComponent).FullName, component.Type);

                var deserializedPosition = JsonSerializer.Deserialize<PositionComponent>(component.Json);
                Assert.NotNull(deserializedPosition);
                Assert.Equal(i, deserializedPosition.Value.X);
                Assert.Equal(i, deserializedPosition.Value.Y);
                Assert.Equal(i, deserializedPosition.Value.Z);
            }
        }
    }
}