using System.Text.Json;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Logging;
using Shared.Networking.Replication;
using Xunit;

namespace SharedUnitTests.Networking.Replication
{
    public class JsonWorldSnapshotConsumerTests
    {
        private readonly EntityRegistry _registry;
        private readonly JsonWorldSnapshotConsumer _consumer;

        public JsonWorldSnapshotConsumerTests()
        {
            var logger = Substitute.For<ILogger>();
            _registry = new EntityRegistry();
            _consumer = new JsonWorldSnapshotConsumer(_registry, logger);
        }

        [Fact]
        public void ConsumeSnapshot_WithValidJson_CreatesEntitiesWithComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var snapshot = CreateSnapshotWithPositionComponent(entityId, 1.5f, 2.5f, 3.5f);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Single(entities);

            var entity = entities.First();
            Assert.Equal(entityId, entity.Id.Value);
            Assert.True(entity.Has<PositionComponent>());

            entity.TryGet<PositionComponent>(out var position);
            Assert.NotNull(position);
            Assert.Equal(1.5f, position.Value.X);
            Assert.Equal(2.5f, position.Value.Y);
            Assert.Equal(3.5f, position.Value.Z);
        }

        [Fact]
        public void ConsumeSnapshot_WithHealthComponent_CreatesEntityWithHealth()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var snapshot = CreateSnapshotWithHealthComponent(entityId, 150);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Single(entities);

            var entity = entities.First();
            Assert.Equal(entityId, entity.Id.Value);
            Assert.True(entity.Has<HealthComponent>());

            entity.TryGet<HealthComponent>(out var health);
            Assert.NotNull(health);
            Assert.Equal(150, health.MaxHealth);
            Assert.Equal(150, health.CurrentHealth);
        }

        [Fact]
        public void ConsumeSnapshot_WithMultipleComponents_CreatesEntityWithAllComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var snapshot = CreateSnapshotWithMultipleComponents(entityId, 10.0f, 20.0f, 30.0f, 200);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Single(entities);

            var entity = entities.First();
            Assert.Equal(entityId, entity.Id.Value);
            Assert.True(entity.Has<PositionComponent>());
            Assert.True(entity.Has<HealthComponent>());

            entity.TryGet<PositionComponent>(out var position);
            Assert.NotNull(position);
            Assert.Equal(10.0f, position.Value.X);
            Assert.Equal(20.0f, position.Value.Y);
            Assert.Equal(30.0f, position.Value.Z);

            entity.TryGet<HealthComponent>(out var health);
            Assert.NotNull(health);
            Assert.Equal(200, health.MaxHealth);
            Assert.Equal(200, health.CurrentHealth);
        }

        [Fact]
        public void ConsumeSnapshot_WithMultipleEntities_CreatesAllEntities()
        {
            // Arrange
            var entityId1 = Guid.NewGuid();
            var entityId2 = Guid.NewGuid();
            var snapshot = CreateSnapshotWithMultipleEntities(entityId1, entityId2);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Equal(2, entities.Count);

            var entity1 = entities.First(e => e.Id.Value == entityId1);
            var entity2 = entities.First(e => e.Id.Value == entityId2);

            Assert.True(entity1.Has<PositionComponent>());
            Assert.True(entity2.Has<HealthComponent>());
        }

        [Fact]
        public void ConsumeSnapshot_WithInvalidComponentType_IgnoresInvalidComponent()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var snapshot = CreateSnapshotWithInvalidComponentType(entityId);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Single(entities);

            var entity = entities.First();
            Assert.Equal(entityId, entity.Id.Value);
            Assert.False(entity.Has<PositionComponent>()); // Should not have the component due to invalid type
        }

        [Fact]
        public void ConsumeSnapshot_WithEmptySnapshot_HandlesGracefully()
        {
            // Arrange
            var initialCount = _registry.GetAll().Count();

            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => _consumer.ConsumeSnapshot(new byte[0]));
            Assert.Null(exception);

            // Assert
            var finalCount = _registry.GetAll().Count();
            Assert.Equal(initialCount, finalCount);
        }

        [Fact]
        public void ConsumeSnapshot_WithExistingEntity_ReplacesComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = _registry.GetOrCreate(entityId);
            entity.AddComponent(new PositionComponent(new System.Numerics.Vector3(1.0f, 1.0f, 1.0f)));

            var snapshot = CreateSnapshotWithPositionComponent(entityId, 5.0f, 5.0f, 5.0f);

            // Act
            _consumer.ConsumeSnapshot(snapshot);

            // Assert
            var updatedEntity = _registry.GetAll().First(e => e.Id.Value == entityId);
            updatedEntity.TryGet<PositionComponent>(out var position);
            Assert.NotNull(position);
            Assert.Equal(5.0f, position.Value.X);
            Assert.Equal(5.0f, position.Value.Y);
            Assert.Equal(5.0f, position.Value.Z);
        }

        private byte[] CreateSnapshotWithPositionComponent(Guid entityId, float x, float y, float z)
        {
            var positionComponent = new PositionComponent(new System.Numerics.Vector3(x, y, z));
            var positionJson = JsonSerializer.Serialize(positionComponent);
        
            var snapshot = new WorldSnapshotMessage
            {
                Entities =
                {
                    new SnapshotEntity
                    {
                        Id = entityId,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = typeof(PositionComponent).FullName!,
                                Json = positionJson
                            }
                        }
                    }
                }
            };

            return System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
        }

        private byte[] CreateSnapshotWithHealthComponent(Guid entityId, int maxHealth)
        {
            var snapshot = new WorldSnapshotMessage
            {
                Entities =
                {
                    new SnapshotEntity
                    {
                        Id = entityId,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = typeof(HealthComponent).FullName!,
                                Json = JsonSerializer.Serialize(new HealthComponent(maxHealth))
                            }
                        }
                    }
                }
            };

            return System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
        }

        private byte[] CreateSnapshotWithMultipleComponents(Guid entityId, float x, float y, float z, int maxHealth)
        {
            var snapshot = new WorldSnapshotMessage
            {
                Entities =
                {
                    new SnapshotEntity
                    {
                        Id = entityId,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = typeof(PositionComponent).FullName!,
                                Json = JsonSerializer.Serialize(new PositionComponent(new System.Numerics.Vector3(x, y, z)))
                            },
                            new SnapshotComponent
                            {
                                Type = typeof(HealthComponent).FullName!,
                                Json = JsonSerializer.Serialize(new HealthComponent(maxHealth))
                            }
                        }
                    }
                }
            };

            return System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
        }

        private byte[] CreateSnapshotWithMultipleEntities(Guid entityId1, Guid entityId2)
        {
            var snapshot = new WorldSnapshotMessage
            {
                Entities =
                {
                    new SnapshotEntity
                    {
                        Id = entityId1,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = typeof(PositionComponent).FullName!,
                                Json = JsonSerializer.Serialize(new PositionComponent(new System.Numerics.Vector3(1.0f, 2.0f, 3.0f)))
                            }
                        }
                    },
                    new SnapshotEntity
                    {
                        Id = entityId2,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = typeof(HealthComponent).FullName!,
                                Json = JsonSerializer.Serialize(new HealthComponent(100))
                            }
                        }
                    }
                }
            };

            return System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
        }

        private byte[] CreateSnapshotWithInvalidComponentType(Guid entityId)
        {
            var snapshot = new WorldSnapshotMessage
            {
                Entities =
                {
                    new SnapshotEntity
                    {
                        Id = entityId,
                        Components =
                        {
                            new SnapshotComponent
                            {
                                Type = "InvalidComponentType",
                                Json = "{}"
                            }
                        }
                    }
                }
            };

            return System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
        }
    }
} 