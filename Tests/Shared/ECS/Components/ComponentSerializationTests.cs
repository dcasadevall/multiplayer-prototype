using System;
using System.Numerics;
using System.Text.Json;
using Shared.ECS.Components;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Components
{
    public class ComponentSerializationTests
    {
        private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

        public static IEnumerable<object[]> RotationTestData()
        {
            yield return [Quaternion.Identity]; // No rotation
            yield return [Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, 0, 0)]; // 90 degrees on Y
            yield return [Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 2, 0)]; // 90 degrees on X
            yield return [Quaternion.CreateFromYawPitchRoll(0, 0, MathF.PI / 2)]; // 90 degrees on Z
            yield return [Quaternion.CreateFromYawPitchRoll(MathF.PI, 0, 0)]; // 180 degrees
            yield return [Quaternion.CreateFromYawPitchRoll(MathF.PI / 4, 0, 0)]; // Original 45 degrees
            yield return [Quaternion.Normalize(new Quaternion(0.5f, -0.2f, 0.8f, 0.1f))]; // Arbitrary rotation
        }

        [Theory]
        [MemberData(nameof(RotationTestData))]
        public void RotationComponent_SerializesAndDeserializesCorrectly(Quaternion rotation)
        {
            // Arrange
            var original = new RotationComponent { Value = rotation };

            // Act
            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<RotationComponent>(json, _options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.X, deserialized.X, 5);
            Assert.Equal(original.Y, deserialized.Y, 5);
            Assert.Equal(original.Z, deserialized.Z, 5);
            Assert.Equal(original.W, deserialized.W, 5);
        }

        [Fact]
        public void PositionComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new PositionComponent { X = 1.23f, Y = 4.56f, Z = 7.89f };

            // Act
            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<PositionComponent>(json, _options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.X, deserialized.X, 5);
            Assert.Equal(original.Y, deserialized.Y, 5);
            Assert.Equal(original.Z, deserialized.Z, 5);
        }

        [Fact]
        public void VelocityComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new VelocityComponent { X = -9.87f, Y = -6.54f, Z = -3.21f };

            // Act
            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<VelocityComponent>(json, _options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.X, deserialized.X, 5);
            Assert.Equal(original.Y, deserialized.Y, 5);
            Assert.Equal(original.Z, deserialized.Z, 5);
        }

        [Fact]
        public void LocalBoundsComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new LocalBoundsComponent
            {
                Center = new Vector3(0.1f, 0.2f, 0.3f),
                Size = new Vector3(1.1f, 1.2f, 1.3f)
            };

            // Act
            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<LocalBoundsComponent>(json, _options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.CenterX, deserialized.CenterX, 5);
            Assert.Equal(original.CenterY, deserialized.CenterY, 5);
            Assert.Equal(original.CenterZ, deserialized.CenterZ, 5);
            Assert.Equal(original.SizeX, deserialized.SizeX, 5);
            Assert.Equal(original.SizeY, deserialized.SizeY, 5);
            Assert.Equal(original.SizeZ, deserialized.SizeZ, 5);
        }

        [Fact]
        public void WorldAABBComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new WorldAABBComponent
            {
                Min = new Vector3(-1f, -2f, -3f),
                Max = new Vector3(1f, 2f, 3f)
            };

            // Act
            var json = JsonSerializer.Serialize(original, _options);
            var deserialized = JsonSerializer.Deserialize<WorldAABBComponent>(json, _options);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.MinX, deserialized.MinX, 5);
            Assert.Equal(original.MinY, deserialized.MinY, 5);
            Assert.Equal(original.MinZ, deserialized.MinZ, 5);
            Assert.Equal(original.MaxX, deserialized.MaxX, 5);
            Assert.Equal(original.MaxY, deserialized.MaxY, 5);
            Assert.Equal(original.MaxZ, deserialized.MaxZ, 5);
        }
    }
}