using System.Numerics;
using System.Text.Json;
using Server.Scenes;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Physics;
using Xunit;

namespace ServerUnitTests
{
    public class SceneLoaderTests
    {
        private static SceneLoader CreateSceneLoader(EntityRegistry registry)
        {
            return new SceneLoader(registry);
        }

        [Fact]
        public void Load_WithBotArchetype_ShouldCreateBotEntity()
        {
            // Arrange
            var registry = new EntityRegistry();
            var loader = CreateSceneLoader(registry);
            var json = @"[
            {
                ""archetype"": ""Bot"",
                ""components"": {
                    ""PositionComponent"": {
                        ""x"": 1.0,
                        ""y"": 2.0,
                        ""z"": 3.0
                    }
                }
            }
        ]";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, json);

            try
            {
                // Act
                loader.Load(tempFile);

                // Assert
                var entities = registry.With<BotTagComponent>().ToList();
                Assert.Single(entities);

                var entity = entities.First();
                Assert.True(entity.Has<PositionComponent>());
                Assert.True(entity.Has<HealthComponent>());

                entity.TryGet<PositionComponent>(out var position);
                Assert.Equal(new Vector3(1, 2, 3), position.Value);
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