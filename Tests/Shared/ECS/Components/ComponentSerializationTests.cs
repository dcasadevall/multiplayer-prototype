using System.Numerics;
using LiteNetLib.Utils;
using Shared.ECS.Replication;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Components
{
    public class ComponentSerializationTests
    {
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
            var writer = new NetDataWriter();
            var reader = new NetDataReader();
            var binarySerializer = new BinaryComponentSerializer(new ComponentTypeRegistry());
            var componentWriter = new NetDataWriterAdapter(writer, binarySerializer);
            var componentReader = new NetDataReaderAdapter(reader, binarySerializer);

            // Act
            original.Serialize(componentWriter);
            reader.SetSource(writer);
            var deserialized = new RotationComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Value.X, deserialized.Value.X, 5);
            Assert.Equal(original.Value.Y, deserialized.Value.Y, 5);
            Assert.Equal(original.Value.Z, deserialized.Value.Z, 5);
            Assert.Equal(original.Value.W, deserialized.Value.W, 5);
        }

        [Fact]
        public void PositionComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new PositionComponent { Value = new Vector3(1.23f, 4.56f, 7.89f) };
            var writer = new NetDataWriter();
            var reader = new NetDataReader();
            var binarySerializer = new BinaryComponentSerializer(new ComponentTypeRegistry());
            var componentWriter = new NetDataWriterAdapter(writer, binarySerializer);
            var componentReader = new NetDataReaderAdapter(reader, binarySerializer);

            // Act
            original.Serialize(componentWriter);
            reader.SetSource(writer);
            var deserialized = new PositionComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Value, deserialized.Value);
        }

        [Fact]
        public void VelocityComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new VelocityComponent { Value = new Vector3(-9.87f, -6.54f, -3.21f) };
            var writer = new NetDataWriter();
            var reader = new NetDataReader();
            var binarySerializer = new BinaryComponentSerializer(new ComponentTypeRegistry());
            var componentWriter = new NetDataWriterAdapter(writer, binarySerializer);
            var componentReader = new NetDataReaderAdapter(reader, binarySerializer);

            // Act
            original.Serialize(componentWriter);
            reader.SetSource(writer);
            var deserialized = new VelocityComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Value, deserialized.Value);
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
            var writer = new NetDataWriter();
            var reader = new NetDataReader();
            var binarySerializer = new BinaryComponentSerializer(new ComponentTypeRegistry());
            var componentWriter = new NetDataWriterAdapter(writer, binarySerializer);
            var componentReader = new NetDataReaderAdapter(reader, binarySerializer);

            // Act
            original.Serialize(componentWriter);
            reader.SetSource(writer);
            var deserialized = new LocalBoundsComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Center, deserialized.Center);
            Assert.Equal(original.Size, deserialized.Size);
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
            var writer = new NetDataWriter();
            var reader = new NetDataReader();
            var binarySerializer = new BinaryComponentSerializer(new ComponentTypeRegistry());
            var componentWriter = new NetDataWriterAdapter(writer, binarySerializer);
            var componentReader = new NetDataReaderAdapter(reader, binarySerializer);

            // Act
            original.Serialize(componentWriter);
            reader.SetSource(writer);
            var deserialized = new WorldAABBComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Min, deserialized.Min);
            Assert.Equal(original.Max, deserialized.Max);
        }
    }
}