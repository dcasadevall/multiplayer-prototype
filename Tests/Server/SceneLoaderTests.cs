using System.Numerics;
using System.Text.Json;
using NSubstitute;
using Server.Scenes;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Networking.Replication;
using Shared.Logging;
using Xunit;

namespace ServerUnitTests
{
    public class SceneLoaderTests
    {
        private static SceneLoader CreateSceneLoader(EntityRegistry registry)
        {
            // Use a dummy logger and the real JsonWorldSnapshotConsumer for tests
            var logger = Substitute.For<ILogger>();
            var consumer = new JsonWorldSnapshotConsumer(registry, logger);
            return new SceneLoader(consumer);
        }
    
        [Fact]
        public void Load_WithValidJson_ShouldCreateEntities()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = @"[
            {
                ""components"": {
                    ""PositionComponent"": {
                        ""x"": 1.0,
                        ""y"": 2.0,
                        ""z"": 3.0
                    },
                    ""HealthComponent"": {
                        ""maxHealth"": 100
                    }
                },
                ""tags"": [""PlayerSpawn""]
            }
        ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.GetAll();
                var collection = entities as Entity[] ?? entities.ToArray();
                Assert.Single(collection);

                var entity = collection.First();
                Assert.True(entity.Has<PositionComponent>());
                Assert.True(entity.Has<HealthComponent>());

                entity.TryGet<PositionComponent>(out var position);
                Assert.Equal(new Vector3(1, 2, 3), position.Value);

                entity.TryGet<HealthComponent>(out var health);
                Assert.Equal(100, health.MaxHealth);
                Assert.Equal(100, health.CurrentHealth);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithMultipleEntities_ShouldCreateAllEntities()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = @"[
            {
                ""components"": {
                    ""PositionComponent"": {
                        ""x"": 0.0,
                        ""y"": 0.0,
                        ""z"": 0.0
                    }
                },
                ""tags"": []
            },
            {
                ""components"": {
                    ""PositionComponent"": {
                        ""x"": 10.0,
                        ""y"": 20.0,
                        ""z"": 30.0
                    },
                    ""HealthComponent"": {
                        ""maxHealth"": 50
                    }
                },
                ""tags"": [""Enemy""]
            }
        ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.GetAll().ToList();
                Assert.Equal(2, entities.Count);

                // First entity should only have position
                var entity1 = entities[0];
                Assert.True(entity1.Has<PositionComponent>());
                Assert.False(entity1.Has<HealthComponent>());

                entity1.TryGet<PositionComponent>(out var position1);
                Assert.Equal(Vector3.Zero, position1.Value);

                // Second entity should have both position and health
                var entity2 = entities[1];
                Assert.True(entity2.Has<PositionComponent>());
                Assert.True(entity2.Has<HealthComponent>());

                entity2.TryGet<PositionComponent>(out var position2);
                Assert.Equal(new Vector3(10, 20, 30), position2.Value);

                entity2.TryGet<HealthComponent>(out var health2);
                Assert.Equal(50, health2.MaxHealth);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithEmptyJson_ShouldNotCreateEntities()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = "[]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.GetAll();
                Assert.Empty(entities);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithOnlyPositionComponent_ShouldCreateEntityWithPosition()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = @"[
            {
                ""components"": {
                    ""PositionComponent"": {
                        ""x"": 5.5,
                        ""y"": 10.5,
                        ""z"": 15.5
                    }
                },
                ""tags"": []
            }
        ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.GetAll().ToList();
                Assert.Single(entities);

                var entity = entities.First();
            
                Assert.True(entity.Has<PositionComponent>());
                Assert.False(entity.Has<HealthComponent>());

                entity.TryGet<PositionComponent>(out var position);
                Assert.Equal(new Vector3(5.5f, 10.5f, 15.5f), position.Value);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithOnlyHealthComponent_ShouldCreateEntityWithHealth()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = @"[
            {
                ""components"": {
                    ""HealthComponent"": {
                        ""maxHealth"": 200
                    }
                },
                ""tags"": []
            }
        ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.GetAll().ToList();
                Assert.Single(entities);

                var entity = entities.First();
                Assert.False(entity.Has<PositionComponent>());
                Assert.True(entity.Has<HealthComponent>());

                entity.TryGet<HealthComponent>(out var health);
                Assert.Equal(200, health.MaxHealth);
                Assert.Equal(200, health.CurrentHealth);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var invalidJson = "{ invalid json }";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, invalidJson);

            try
            {
                // Act & Assert
                Assert.Throws<JsonException>(() => loader.Load(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Load_WithNonExistentFile_ShouldThrowException()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var nonExistentPath = "non_existent_file.json";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => loader.Load(nonExistentPath));
        }
    }
}