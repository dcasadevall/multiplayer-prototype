using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class TestGuidComponent : IComponent
    {
        public System.Guid Value { get; set; }
        public void Serialize(IComponentWriter writer) => writer.Put(Value);
        public void Deserialize(IComponentReader reader) => Value = reader.GetGuid();
    }

    public class BinaryComponentSerializerTests
    {
        [Fact]
        public void SerializeAndDeserialize_PositionComponent_ReturnsEqualComponent()
        {
            // Arrange
            var componentTypeRegistry = new ComponentTypeRegistry();
            var serializer = new BinaryComponentSerializer(componentTypeRegistry);
            var originalComponent = new PositionComponent { Value = new Vector3(1, 2, 3) };

            // Act
            var serializedData = serializer.Serialize(originalComponent);
            var deserializedComponent = (PositionComponent)serializer.Deserialize(serializedData);

            // Assert
            Assert.Equal(originalComponent.Value, deserializedComponent.Value);
        }

        [Fact]
        public void SerializeAndDeserialize_TagComponent_ReturnsEqualComponent()
        {
            // Arrange
            var componentTypeRegistry = new ComponentTypeRegistry();
            var serializer = new BinaryComponentSerializer(componentTypeRegistry);
            var originalComponent = new CollidingTagComponent();

            // Act
            var serializedData = serializer.Serialize(originalComponent);
            var deserializedComponent = (CollidingTagComponent)serializer.Deserialize(serializedData);

            // Assert
            Assert.NotNull(deserializedComponent);
            Assert.IsType<CollidingTagComponent>(deserializedComponent);
        }

        [Fact]
        public void SerializeAndDeserialize_ComponentWithGuid_ReturnsEqualComponent()
        {
            // Arrange
            var componentTypeRegistry = new ComponentTypeRegistry();
            var serializer = new BinaryComponentSerializer(componentTypeRegistry);
            var originalComponent = new TestGuidComponent { Value = System.Guid.NewGuid() };

            // Act
            var serializedData = serializer.Serialize(originalComponent);
            var deserializedComponent = (TestGuidComponent)serializer.Deserialize(serializedData);

            // Assert
            Assert.Equal(originalComponent.Value, deserializedComponent.Value);
        }
    }
}