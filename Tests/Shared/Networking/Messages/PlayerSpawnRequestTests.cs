using System;
using System.Numerics;
using System.Text.Json;
using Shared.Networking.Messages;
using Xunit;

namespace Tests.Shared.Networking.Messages
{
    public class PlayerSpawnRequestTests
    {
        [Fact]
        public void PlayerSpawnRequest_Serialization_WorksCorrectly()
        {
            // Arrange
            var position = new Vector3(1.5f, 2.5f, 3.5f);
            var playerName = "TestPlayer";
            var request = new PlayerSpawnRequest(position, playerName);

            // Act
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var deserialized = JsonSerializer.Deserialize<PlayerSpawnRequest>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("PlayerSpawnRequest", deserialized.Type);
            Assert.Equal(position, deserialized.Position);
            Assert.Equal(playerName, deserialized.PlayerName);
            Assert.True(deserialized.Timestamp > 0);
        }

        [Fact]
        public void PlayerSpawnRequest_DefaultConstructor_SetsCorrectType()
        {
            // Arrange & Act
            var request = new PlayerSpawnRequest();

            // Assert
            Assert.Equal("PlayerSpawnRequest", request.Type);
            Assert.Equal(Vector3.Zero, request.Position);
            Assert.Equal(string.Empty, request.PlayerName);
        }

        [Fact]
        public void PlayerSpawnRequest_WithPositionOnly_SetsCorrectValues()
        {
            // Arrange
            var position = new Vector3(10f, 20f, 30f);

            // Act
            var request = new PlayerSpawnRequest(position);

            // Assert
            Assert.Equal("PlayerSpawnRequest", request.Type);
            Assert.Equal(position, request.Position);
            Assert.Equal(string.Empty, request.PlayerName);
        }
    }
} 