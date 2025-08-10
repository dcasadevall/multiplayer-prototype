using System;
using System.Collections.Generic;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.Networking;
using Shared.Physics;
using NSubstitute;
using Xunit;
using System.Numerics;
using Shared.Prediction;
using Shared.Replication;

namespace SharedUnitTests.ECS.Replication
{
    public class ClientReplicationSystemTests
    {
        private readonly EntityRegistry _registry;
        private readonly ClientReplicationSystem _system;
        private readonly IComponentSerializer _componentSerializer;
        private readonly ComponentTypeRegistry _componentRegistry;
        private MessageHandler<WorldDeltaMessage> _messageHandler = null!;

        public ClientReplicationSystemTests()
        {
            _registry = new EntityRegistry();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();
            _componentSerializer = Substitute.For<IComponentSerializer>();
            _componentRegistry = new ComponentTypeRegistry();

            // For most tests, we start with an empty snapshot.
            connection.InitialWorldSnapshot.Returns(new WorldDeltaMessage(_componentSerializer, _componentRegistry));

            // Capture the message handler that the system registers in its constructor.
            messageReceiver.RegisterMessageHandler(Arg.Any<string>(),
                Arg.Do<MessageHandler<WorldDeltaMessage>>(handler => _messageHandler = handler));

            _system = new ClientReplicationSystem(messageReceiver, connection);
        }

        [Fact]
        public void Update_WhenInitialized_ConsumesInitialWorldSnapshot()
        {
            // Arrange
            var registry = new EntityRegistry();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();
            var entityId = Guid.NewGuid();
            var initialSnapshot = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta> { new() { EntityId = entityId, IsNew = true } }
            };
            connection.InitialWorldSnapshot.Returns(initialSnapshot);
            var system = new ClientReplicationSystem(messageReceiver, connection);

            // Act
            system.Update(registry, 0, 0);

            // Assert
            Assert.True(registry.TryGet(new EntityId(entityId), out _));
        }

        [Fact]
        public void Update_WhenDeltaReceived_CreatesEntityWithComponents()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var deltaMessage = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entityId,
                        IsNew = true,
                        AddedOrModifiedComponents = new List<IComponent> { new PositionComponent() }
                    }
                }
            };

            // Act
            _messageHandler.Invoke(0, deltaMessage);
            _system.Update(_registry, 1, 0);

            // Assert
            Assert.True(_registry.TryGet(new EntityId(entityId), out var entity));
            Assert.True(entity.Has<PositionComponent>());
        }

        [Fact]
        public void Update_WhenDeltaReceived_DestroysEntity()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var deltaMessage = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta> { new() { EntityId = entity.Id.Value, IsDestroyed = true } }
            };

            // Act
            _messageHandler.Invoke(0, deltaMessage);
            _system.Update(_registry, 1, 0);

            // Assert
            Assert.False(_registry.TryGet(entity.Id, out _));
        }

        [Fact]
        public void Update_WhenDeltaReceived_UpdatesComponent()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new PositionComponent(new(1, 2, 3)));
            var deltaMessage = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entity.Id.Value,
                        IsNew = false,
                        AddedOrModifiedComponents = new List<IComponent> { new PositionComponent(new(4, 5, 6)) }
                    }
                }
            };

            // Act
            _messageHandler.Invoke(0, deltaMessage);
            _system.Update(_registry, 1, 0);

            // Assert
            Assert.Equal(new Vector3(4, 5, 6), entity.Get<PositionComponent>()!.Value);
        }

        [Fact]
        public void Update_WhenDeltaReceived_RemovesComponent()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new PositionComponent());
            var deltaMessage = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entity.Id.Value,
                        IsNew = false,
                        RemovedComponents = new List<Type> { typeof(PositionComponent) }
                    }
                }
            };

            // Act
            _messageHandler.Invoke(0, deltaMessage);
            _system.Update(_registry, 1, 0);

            // Assert
            Assert.False(entity.Has<PositionComponent>());
        }

        [Fact]
        public void Update_WhenPredictedDeltaReceived_SetsServerValue()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var predicted = new PredictedComponent<PositionComponent>();
            entity.AddComponent(predicted);

            var serverAuth = new PositionComponent { Value = new Vector3(1, 2, 3) };
            var deltaMessage = new WorldDeltaMessage(_componentSerializer, _componentRegistry)
            {
                Deltas = new List<EntityDelta>
                {
                    new()
                    {
                        EntityId = entity.Id.Value,
                        AddedOrModifiedComponents =
                            { new PredictedComponent<PositionComponent> { Mode = ReplicationMode.EveryTick, ServerValue = serverAuth } }
                    }
                }
            };

            // Act
            _messageHandler.Invoke(0, deltaMessage);
            _system.Update(_registry, 1, 0);

            // Assert
            // Assert.NotNull(predicted.ServerValue);
            Assert.Equal(serverAuth.Value, entity.GetRequired<PredictedComponent<PositionComponent>>().ServerValue!.Value);
        }
    }
}