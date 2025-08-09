using System.Numerics;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Entities
{
    public class TestServerComponent : IComponent, IServerComponent { }

    public class EntityRegistryTests
    {
        [Fact]
        public void ProduceEntityDelta_WithNewEntity_CreatesDelta()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));

            // Act
            var deltas = registry.ProduceEntityDelta();

            // Assert
            Assert.Single(deltas);
            var delta = deltas.First();
            Assert.Equal(entity.Id.Value, delta.EntityId);
            Assert.True(delta.IsNew);
            Assert.Single(delta.AddedOrModifiedComponents);
        }

        [Fact]
        public void ProduceEntityDelta_WithModifiedEntity_CreatesDelta()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            registry.ProduceEntityDelta();
            entity.AddOrReplaceComponent(new PositionComponent(new(4, 5, 6)));

            // Act
            var deltas = registry.ProduceEntityDelta();

            // Assert
            Assert.Single(deltas);
            var delta = deltas.First();
            Assert.False(delta.IsNew);
            Assert.Single(delta.AddedOrModifiedComponents);
        }

        [Fact]
        public void ProduceEntityDelta_WithRemovedComponent_CreatesDelta()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            registry.ProduceEntityDelta();
            entity.Remove<PositionComponent>();

            // Act
            var deltas = registry.ProduceEntityDelta();

            // Assert
            Assert.Single(deltas);
            var delta = deltas.First();
            Assert.False(delta.IsNew);
            Assert.Empty(delta.AddedOrModifiedComponents);
            Assert.Single(delta.RemovedComponents);
            Assert.Equal(typeof(PositionComponent), delta.RemovedComponents.First());
        }

        [Fact]
        public void ProduceEntityDelta_WithDestroyedEntity_SetsIsDestroyed()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            registry.ProduceEntityDelta();
            registry.DestroyEntity(entity.Id);

            // Act
            var deltas = registry.ProduceEntityDelta();

            // Assert
            Assert.Single(deltas);
            var delta = deltas.First();
            Assert.True(delta.IsDestroyed);
            Assert.False(delta.IsNew);
            Assert.Empty(delta.AddedOrModifiedComponents);
            Assert.Empty(delta.RemovedComponents);
        }

        [Fact]
        public void ConsumeEntityDelta_WithNewEntity_CreatesEntity()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entityId = Guid.NewGuid();
            var deltas = new[]
            {
                new EntityDelta
                {
                    EntityId = entityId,
                    IsNew = true,
                    AddedOrModifiedComponents = new List<IComponent> { new PositionComponent(new(1, 2, 3)) }
                }
            }.ToList();

            // Act
            registry.ConsumeEntityDelta(deltas);

            // Assert
            Assert.True(registry.TryGet(new EntityId(entityId), out var entity));
            Assert.True(entity.Has<PositionComponent>());
        }

        [Fact]
        public void ConsumeEntityDelta_WithModifiedEntity_UpdatesComponent()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            var deltas = new[]
            {
                new EntityDelta
                {
                    EntityId = entity.Id.Value,
                    IsNew = false,
                    AddedOrModifiedComponents = new List<IComponent> { new PositionComponent(new(4, 5, 6)) }
                }
            }.ToList();

            // Act
            registry.ConsumeEntityDelta(deltas);

            // Assert
            Assert.Equal(new Vector3(4, 5, 6), entity.Get<PositionComponent>()!.Value);
        }

        [Fact]
        public void ConsumeEntityDelta_WithRemovedComponent_RemovesComponent()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            var deltas = new[]
            {
                new EntityDelta
                {
                    EntityId = entity.Id.Value,
                    IsNew = false,
                    RemovedComponents = new List<Type> { typeof(PositionComponent) }
                }
            }.ToList();

            // Act
            registry.ConsumeEntityDelta(deltas);

            // Assert
            Assert.False(entity.Has<PositionComponent>());
        }

        [Fact]
        public void ConsumeEntityDelta_WithDestroyedEntity_RemovesEntity()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            var deltas = new[]
            {
                new EntityDelta
                {
                    EntityId = entity.Id.Value,
                    IsDestroyed = true
                }
            }.ToList();

            // Act
            registry.ConsumeEntityDelta(deltas);

            // Assert
            Assert.False(registry.TryGet(entity.Id, out _));
        }

        [Fact]
        public void ProduceEntityDelta_WithServerComponent_ExcludesServerComponent()
        {
            // Arrange
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            entity.AddComponent(new TestServerComponent());

            // Act
            var deltas = registry.ProduceEntityDelta();

            // Assert
            Assert.Single(deltas);
            var delta = deltas.First();
            Assert.Single(delta.AddedOrModifiedComponents);
            Assert.IsType<PositionComponent>(delta.AddedOrModifiedComponents.First());
        }
    }
}