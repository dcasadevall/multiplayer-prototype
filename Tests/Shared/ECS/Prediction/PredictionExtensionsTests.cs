using System;
using System.Numerics;
using Shared.ECS.Entities;
using Shared.Physics;
using Xunit;

namespace Shared.ECS.Prediction.Tests
{
    public class PredictionExtensionsTests
    {
        [Fact]
        public void AddPredictedComponent_AddsBothPredictedAndBaseComponents()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            var component = new PositionComponent(new Vector3(1, 2, 3));

            // Act
            entity.AddPredictedComponent(component);

            // Assert
            Assert.True(entity.Has<PredictedComponent<PositionComponent>>());
            Assert.True(entity.Has<PositionComponent>());
            Assert.Equal(new Vector3(1, 2, 3), entity.GetRequired<PositionComponent>().Value);
        }

        [Fact]
        public void TrySetServerAuthoritativeValue_SetsValueOnPredictedComponent()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            var initialComponent = new PositionComponent(new Vector3(1, 1, 1));
            var serverComponent = new PositionComponent(new Vector3(2, 2, 2));
            entity.AddPredictedComponent(initialComponent);

            // Act
            var result = entity.TrySetServerAuthoritativeValue(typeof(PositionComponent), serverComponent);
            var predictedWrapper = entity.GetRequired<PredictedComponent<PositionComponent>>();

            // Assert
            Assert.True(result);
            Assert.NotNull(predictedWrapper.ServerValue);
            Assert.Equal(new Vector3(2, 2, 2), predictedWrapper.ServerValue.Value);
        }

        [Fact]
        public void TrySetServerAuthoritativeValue_FailsGracefully_WhenComponentNotFound()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            var serverComponent = new PositionComponent(new Vector3(2, 2, 2));

            // Act
            var result = entity.TrySetServerAuthoritativeValue(typeof(PositionComponent), serverComponent);

            // Assert
            Assert.False(result);
        }
    }
}