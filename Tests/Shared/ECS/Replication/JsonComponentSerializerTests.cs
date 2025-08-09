using System.Text.Json;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class JsonComponentSerializerTests
    {
        [Fact]
        public void SerializeAndDeserialize_SimpleComponent_ReturnsEqualComponent()
        {
            // Arrange
            var serializer = new JsonComponentSerializer();
            var originalComponent = new PositionComponent { Value = new System.Numerics.Vector3(1, 2, 3) };

            // Act
            var data = serializer.Serialize(originalComponent);
            var deserializedComponent = serializer.Deserialize(data);

            // Assert
            Assert.Equal(originalComponent.Value, ((PositionComponent)deserializedComponent).Value);
        }

        [Fact]
        public void SerializeAndDeserialize_GenericComponent_ReturnsEqualComponent()
        {
            // Arrange
            var serializer = new JsonComponentSerializer();
            var originalComponent = new PredictedComponent<PositionComponent>
            {
                ServerValue = new PositionComponent { Value = new System.Numerics.Vector3(1, 2, 3) },
            };

            // Act
            var data = serializer.Serialize(originalComponent);
            var deserializedComponent = serializer.Deserialize(data);

            // Assert
            Assert.True(deserializedComponent is PredictedComponent<PositionComponent>);
            Assert.NotNull(((PredictedComponent<PositionComponent>)deserializedComponent).ServerValue);
            Assert.Equal(originalComponent.ServerValue.Value,
                ((PredictedComponent<PositionComponent>)deserializedComponent).ServerValue!.Value);
        }
    }
}