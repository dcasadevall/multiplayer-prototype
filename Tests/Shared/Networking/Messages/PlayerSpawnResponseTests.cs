using System;
using System.Numerics;
using System.Text.Json;
using Shared.Networking.Messages;
using Xunit;

namespace Tests.Shared.Networking.Messages
{
    public class PlayerSpawnResponseTests
    {
        [Fact]
        public void PlayerSpawnResponse_Serialization_WorksCorrectly()
        {
            // Arrange
            var playerEntityId = Guid.NewGuid();
            var spawnPosition = new Vector3(1.5f, 2.5f, 3.5f);
            var response = new PlayerSpawnResponse(playerEntityId, spawnPosition, true);

            // Act
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var deserialized = JsonSerializer.Deserialize<PlayerSpawnResponse>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("PlayerSpawnResponse", deserialized.Type);
            Assert.Equal(playerEntityId, deserialized.PlayerEntityId);
            Assert.Equal(spawnPosition, deserialized.SpawnPosition);
            Assert.True(deserialized.Success);
            Assert.Equal(string.Empty, deserialized.ErrorMessage);
            Assert.True(deserialized.Timestamp > 0);
        }

        [Fact]
        public void PlayerSpawnResponse_FailureCase_SetsCorrectValues()
        {
            // Arrange
            var errorMessage = "Spawn failed due to invalid position";
            var response = new PlayerSpawnResponse(Guid.Empty, Vector3.Zero, false, errorMessage);

            // Assert
            Assert.Equal("PlayerSpawnResponse", response.Type);
            Assert.Equal(Guid.Empty, response.PlayerEntityId);
            Assert.Equal(Vector3.Zero, response.SpawnPosition);
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.ErrorMessage);
        }

        [Fact]
        public void PlayerSpawnResponse_DefaultConstructor_SetsCorrectType()
        {
            // Arrange & Act
            var response = new PlayerSpawnResponse();

            // Assert
            Assert.Equal("PlayerSpawnResponse", response.Type);
            Assert.Equal(Guid.Empty, response.PlayerEntityId);
            Assert.Equal(Vector3.Zero, response.SpawnPosition);
            Assert.False(response.Success);
            Assert.Equal(string.Empty, response.ErrorMessage);
        }
    }
} 