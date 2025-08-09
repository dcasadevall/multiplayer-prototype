using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Replication;
using Shared.Logging;
using Shared.Physics;
using Xunit;
using Shared.Damage;
using Shared.ECS.Entities;

namespace SharedUnitTests.Networking.Replication
{
    public class JsonWorldDeltaConsumerTests
    {
        private readonly EntityRegistry _registry;
        private readonly JsonWorldDeltaConsumer _consumer;

        public JsonWorldDeltaConsumerTests()
        {
            var logger = Substitute.For<ILogger>();
            _registry = new EntityRegistry();
            _consumer = new JsonWorldDeltaConsumer(_registry, logger);
        }

        [Fact]
        public void ConsumeDelta_WithNewEntity_CreatesEntityWithComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var delta = new WorldDeltaMessage
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entityId,
                        IsNew = true,
                        AddedOrModifiedComponents = new List<IComponent> { new PositionComponent(new(1.5f, 2.5f, 3.5f)) }
                    }
                }
            };

            // Act
            _consumer.ConsumeDelta(delta);

            // Assert
            var entities = _registry.GetAll().ToList();
            Assert.Single(entities);

            var entity = entities.First();
            Assert.Equal(entityId, entity.Id.Value);
            Assert.True(entity.Has<PositionComponent>());

            entity.TryGet<PositionComponent>(out var position);
            Assert.NotNull(position);
            Assert.Equal(1.5f, position.Value.X);
            Assert.Equal(2.5f, position.Value.Y);
            Assert.Equal(3.5f, position.Value.Z);
        }

        [Fact]
        public void ConsumeDelta_WithExistingEntity_AddsAndRemovesComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = _registry.GetOrCreate(entityId);
            entity.AddComponent(new PositionComponent(new(1.0f, 1.0f, 1.0f)));
            entity.AddComponent(new HealthComponent(100));


            var delta = new WorldDeltaMessage
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entityId,
                        AddedOrModifiedComponents = new List<IComponent> { new VelocityComponent(new(5.0f, 5.0f, 5.0f)) },
                        RemovedComponents = new List<Type> { typeof(PositionComponent) }
                    }
                }
            };

            // Act
            _consumer.ConsumeDelta(delta);

            // Assert
            var updatedEntity = _registry.GetAll().First(e => e.Id.Value == entityId);
            Assert.False(updatedEntity.Has<PositionComponent>());
            Assert.True(updatedEntity.Has<VelocityComponent>());
            Assert.True(updatedEntity.Has<HealthComponent>()); // Should still be there

            updatedEntity.TryGet<VelocityComponent>(out var velocity);
            Assert.NotNull(velocity);
            Assert.Equal(5.0f, velocity.Value.X);
            Assert.Equal(5.0f, velocity.Value.Y);
            Assert.Equal(5.0f, velocity.Value.Z);
        }

        [Fact]
        public void ConsumeDelta_WithExistingEntity_ModifiesComponent()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var entity = _registry.GetOrCreate(entityId);
            entity.AddComponent(new PositionComponent(new(1.0f, 1.0f, 1.0f)));

            var delta = new WorldDeltaMessage
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entityId,
                        AddedOrModifiedComponents = new List<IComponent> { new PositionComponent(new(2.0f, 2.0f, 2.0f)) },
                    }
                }
            };

            // Act
            _consumer.ConsumeDelta(delta);

            // Assert
            var updatedEntity = _registry.GetAll().First(e => e.Id.Value == entityId);
            Assert.True(updatedEntity.Has<PositionComponent>());

            updatedEntity.TryGet<PositionComponent>(out var position);
            Assert.NotNull(position);
            Assert.Equal(2.0f, position.Value.X);
        }
    }
}