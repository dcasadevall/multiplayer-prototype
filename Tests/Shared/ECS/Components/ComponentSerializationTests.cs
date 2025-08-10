using System.Numerics;
using LiteNetLib.Utils;
using NSubstitute;
using Shared.ECS.Components;
using Shared.Physics;
using Shared.Replication;
using Xunit;

namespace SharedUnitTests.ECS.Components
{
    public class ComponentSerializationTests
    {
        public static IEnumerable<object[]> RotationTestData()
        {
            yield return new object[] { Quaternion.Identity }; // No rotation
            yield return new object[] { Quaternion.CreateFromYawPitchRoll(MathF.PI / 2, 0, 0) }; // 90 degrees on Y
            yield return new object[] { Quaternion.CreateFromYawPitchRoll(0, MathF.PI / 2, 0) }; // 90 degrees on X
            yield return new object[] { Quaternion.CreateFromYawPitchRoll(0, 0, MathF.PI / 2) }; // 90 degrees on Z
            yield return new object[] { Quaternion.CreateFromYawPitchRoll(MathF.PI, 0, 0) }; // 180 degrees
            yield return new object[] { Quaternion.CreateFromYawPitchRoll(MathF.PI / 4, 0, 0) }; // Original 45 degrees
            yield return new object[] { Quaternion.Normalize(new Quaternion(0.5f, -0.2f, 0.8f, 0.1f)) }; // Arbitrary rotation
        }

        [Theory]
        [MemberData(nameof(RotationTestData))]
        public void RotationComponent_SerializesAndDeserializesCorrectly(Quaternion rotation)
        {
            // Arrange
            var original = new RotationComponent { Value = rotation };
            var writer = new NetDataWriter();
            var componentWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());

            // Act
            original.Serialize(componentWriter);
            var reader = new NetDataReader(writer);
            var componentReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            var deserialized = new RotationComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.InRange(deserialized.Value.X, original.Value.X - 0.001f, original.Value.X + 0.001f);
            Assert.InRange(deserialized.Value.Y, original.Value.Y - 0.001f, original.Value.Y + 0.001f);
            Assert.InRange(deserialized.Value.Z, original.Value.Z - 0.001f, original.Value.Z + 0.001f);
            Assert.InRange(deserialized.Value.W, original.Value.W - 0.001f, original.Value.W + 0.001f);
        }

        [Fact]
        public void PositionComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new PositionComponent { Value = new Vector3(1.23f, 4.56f, 7.89f) };
            var writer = new NetDataWriter();
            var componentWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());

            // Act
            original.Serialize(componentWriter);
            var reader = new NetDataReader(writer);
            var componentReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            var deserialized = new PositionComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.InRange(deserialized.Value.X, original.Value.X - 0.01f, original.Value.X + 0.01f);
            Assert.InRange(deserialized.Value.Y, original.Value.Y - 0.01f, original.Value.Y + 0.01f);
            Assert.InRange(deserialized.Value.Z, original.Value.Z - 0.01f, original.Value.Z + 0.01f);
        }

        [Fact]
        public void VelocityComponent_SerializesAndDeserializesCorrectly()
        {
            // Arrange
            var original = new VelocityComponent { Value = new Vector3(-9.87f, -6.54f, -3.21f) };
            var writer = new NetDataWriter();
            var componentWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());

            // Act
            original.Serialize(componentWriter);
            var reader = new NetDataReader(writer);
            var componentReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            var deserialized = new VelocityComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.InRange(deserialized.Value.X, original.Value.X - 0.01f, original.Value.X + 0.01f);
            Assert.InRange(deserialized.Value.Y, original.Value.Y - 0.01f, original.Value.Y + 0.01f);
            Assert.InRange(deserialized.Value.Z, original.Value.Z - 0.01f, original.Value.Z + 0.01f);
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
            var componentWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());

            // Act
            original.Serialize(componentWriter);
            var reader = new NetDataReader(writer);
            var componentReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            var deserialized = new LocalBoundsComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.InRange(deserialized.Center.X, original.Center.X - 0.01f, original.Center.X + 0.01f);
            Assert.InRange(deserialized.Center.Y, original.Center.Y - 0.01f, original.Center.Y + 0.01f);
            Assert.InRange(deserialized.Center.Z, original.Center.Z - 0.01f, original.Center.Z + 0.01f);
            Assert.InRange(deserialized.Size.X, original.Size.X - 0.01f, original.Size.X + 0.01f);
            Assert.InRange(deserialized.Size.Y, original.Size.Y - 0.01f, original.Size.Y + 0.01f);
            Assert.InRange(deserialized.Size.Z, original.Size.Z - 0.01f, original.Size.Z + 0.01f);
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
            var componentWriter = new NetDataWriterAdapter(writer, Substitute.For<IComponentSerializer>());

            // Act
            original.Serialize(componentWriter);
            var reader = new NetDataReader(writer);
            var componentReader = new NetDataReaderAdapter(reader, Substitute.For<IComponentSerializer>());
            var deserialized = new WorldAABBComponent();
            deserialized.Deserialize(componentReader);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Min, deserialized.Min);
            Assert.Equal(original.Max, deserialized.Max);
        }
    }
}