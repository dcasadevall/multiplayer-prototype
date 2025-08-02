using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.ECS.Replication;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Shared.Networking.Replication
{
    public class WorldSnapshotMessageTests
    {
        private readonly ITestOutputHelper _output;

        public WorldSnapshotMessageTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Serialization_WithComponents_WorksCorrectly()
        {
            // Arrange
            var snapshot = new WorldSnapshotMessage();
            var entityId = Guid.NewGuid();

            snapshot.Entities.Add(new SnapshotEntity
            {
                Id = entityId,
                Components = new()
                {
                    new SnapshotComponent
                    {
                        Type = "Shared.ECS.Components.PositionComponent",
                        Json = "{\"x\":1.5,\"y\":2.5,\"z\":3.5}"
                    }
                }
            });

            // Act - Serialize to JSON
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true // Makes the JSON readable for debugging
            };

            var json = JsonSerializer.Serialize(snapshot, options);
            _output.WriteLine("Serialized JSON:");
            _output.WriteLine(json);

            // Deserialize back
            var deserialized = JsonSerializer.Deserialize<WorldSnapshotMessage>(json, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Single(deserialized.Entities);

            var entity = deserialized.Entities[0];
            Assert.Equal(entityId, entity.Id);
            Assert.Single(entity.Components);

            var component = entity.Components[0];
            Assert.Equal("Shared.ECS.Components.PositionComponent", component.Type);
            Assert.Contains("\"x\":1.5", component.Json);
            Assert.Contains("\"y\":2.5", component.Json);
            Assert.Contains("\"z\":3.5", component.Json);
        }

        [Fact]
        public void Deserialization_WithRawBytes_WorksCorrectly()
        {
            // Arrange - Create a snapshot message
            var snapshot = new WorldSnapshotMessage();
            var entityId = Guid.NewGuid();

            snapshot.Entities.Add(new SnapshotEntity
            {
                Id = entityId,
                Components = new()
                {
                    new SnapshotComponent
                    {
                        Type = "Shared.ECS.Components.PositionComponent",
                        Json = "{\"x\":1.5,\"y\":2.5,\"z\":3.5}"
                    }
                }
            });

            // Act - Serialize to bytes like we do in production
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(snapshot, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Log the raw bytes and string representation
            _output.WriteLine($"Raw bytes length: {bytes.Length}");
            _output.WriteLine($"UTF8 string: {System.Text.Encoding.UTF8.GetString(bytes)}");

            // Deserialize using the same process as JsonWorldSnapshotConsumer
            var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            var deserialized = JsonSerializer.Deserialize<WorldSnapshotMessage>(jsonString, options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Single(deserialized.Entities);

            var entity = deserialized.Entities[0];
            Assert.Equal(entityId, entity.Id);
            Assert.Single(entity.Components);

            var component = entity.Components[0];
            Assert.Equal("Shared.ECS.Components.PositionComponent", component.Type);
            Assert.Contains("\"x\":1.5", component.Json);
            Assert.Contains("\"y\":2.5", component.Json);
            Assert.Contains("\"z\":3.5", component.Json);
        }
    }
}