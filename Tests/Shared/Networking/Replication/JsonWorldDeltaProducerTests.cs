using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Logging;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.Networking.Replication
{
    public class JsonWorldDeltaProducerTests
    {
        private readonly EntityRegistry _registry;
        private readonly JsonWorldDeltaProducer _producer;
        private readonly ILogger _logger;

        public JsonWorldDeltaProducerTests()
        {
            _logger = Substitute.For<ILogger>();
            _registry = new EntityRegistry();
            _producer = new JsonWorldDeltaProducer(_registry, _logger);
        }

        [Fact]
        public void ProduceDelta_WithNewEntity_CreatesDelta()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));

            // Act
            var delta = _producer.ProduceDelta();

            // Assert
            Assert.Single(delta.Deltas);
            var entityDelta = delta.Deltas.First();
            Assert.Equal(entity.Id.Value, entityDelta.EntityId);
            Assert.True(entityDelta.IsNew);
            Assert.Equal(2, entityDelta.AddedOrModifiedComponents.Count);
        }

        [Fact]
        public void ProduceDelta_WithModifiedEntity_CreatesDelta()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            _producer.ProduceDelta(); // First snapshot
            var newPosition = new PositionComponent(new(4, 5, 6));
            entity.AddOrReplaceComponent(newPosition);

            // Act
            var delta = _producer.ProduceDelta();

            // Assert
            Assert.Single(delta.Deltas);
            var entityDelta = delta.Deltas.First();
            Assert.False(entityDelta.IsNew);
            Assert.Single(entityDelta.AddedOrModifiedComponents);
            Assert.Empty(entityDelta.RemovedComponents);
            Assert.Equal(newPosition.Value, ((PositionComponent)entityDelta.AddedOrModifiedComponents.First()).Value);
        }

        [Fact]
        public void ProduceDelta_WithRemovedComponent_CreatesDelta()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            _producer.ProduceDelta();
            entity.Remove<PositionComponent>();

            // Act
            var delta = _producer.ProduceDelta();

            // Assert
            Assert.Single(delta.Deltas);
            var entityDelta = delta.Deltas.First();
            Assert.False(entityDelta.IsNew);
            Assert.Empty(entityDelta.AddedOrModifiedComponents);
            Assert.Single(entityDelta.RemovedComponents);
            Assert.Equal(typeof(PositionComponent), entityDelta.RemovedComponents.First());
        }

        [Fact]
        public void ProduceDelta_WithDestroyedEntity_CreatesDelta()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new ReplicatedTagComponent());
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            _producer.ProduceDelta();
            _registry.DestroyEntity(entity.Id);

            // Act
            var delta = _producer.ProduceDelta();

            // Assert
            Assert.Single(delta.Deltas);
            var entityDelta = delta.Deltas.First();
            Assert.Equal(entity.Id.Value, entityDelta.EntityId);
            Assert.False(entityDelta.IsNew);
            Assert.Empty(entityDelta.AddedOrModifiedComponents);
            Assert.Equal(2, entityDelta.RemovedComponents.Count);
        }
    }
}