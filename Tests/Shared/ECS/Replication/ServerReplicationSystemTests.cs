using System;
using System.Collections.Generic;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Physics;
using NSubstitute;
using Xunit;
using System.Linq;

namespace SharedUnitTests.ECS.Replication
{
    public class ServerReplicationSystemTests
    {
        private readonly EntityRegistry _registry;
        private readonly IMessageSender _messageSender;
        private readonly MessageFactory _messageFactory;
        private readonly ServerReplicationSystem _system;

        public ServerReplicationSystemTests()
        {
            _registry = new EntityRegistry();
            _messageSender = Substitute.For<IMessageSender>();
            var componentSerializer = Substitute.For<IComponentSerializer>();
            var componentRegistry = new ComponentTypeRegistry();
            _messageFactory = new MessageFactory(componentSerializer, componentRegistry);
            var logger = Substitute.For<ILogger>();
            _system = new ServerReplicationSystem(_registry, _messageSender, _messageFactory, logger);
            _system.Initialize();
        }

        [Fact]
        public void Update_WhenEntityCreated_ProducesDeltaWithIsNew()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            entity.AddComponent(new PositionComponent());
            
            // Act
            _system.Update(_registry, 0, 0);

            // Assert
            _messageSender.Received().BroadcastMessage(
                Arg.Is(MessageType.Delta),
                Arg.Is<WorldDeltaMessage>(m => 
                    m.Deltas.Count == 1 &&
                    m.Deltas[0].IsNew &&
                    m.Deltas[0].EntityId == entity.Id.Value &&
                    m.Deltas[0].AddedOrModifiedComponents.Count == 1),
                Arg.Is(ChannelType.ReliableOrdered));
        }

        [Fact]
        public void Update_WhenComponentModified_ProducesDeltaWithComponent()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            _system.Update(_registry, 0, 0); // Clear created state
            entity.AddComponent(new PositionComponent());

            // Act
            _system.Update(_registry, 1, 0);

            // Assert
             _messageSender.Received().BroadcastMessage(
                Arg.Is(MessageType.Delta),
                Arg.Is<WorldDeltaMessage>(m => 
                    m.Deltas.Count == 1 &&
                    !m.Deltas[0].IsNew &&
                    m.Deltas[0].AddedOrModifiedComponents.Count == 1),
                Arg.Is(ChannelType.ReliableOrdered));
        }

        [Fact]
        public void Update_WhenEntityDestroyed_ProducesDeltaWithIsDestroyed()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            _system.Update(_registry, 0, 0); // Clear created state
            _registry.DestroyEntity(entity.Id);

            // Act
            _system.Update(_registry, 1, 0);

            // Assert
             _messageSender.Received().BroadcastMessage(
                Arg.Is(MessageType.Delta),
                Arg.Is<WorldDeltaMessage>(m => 
                    m.Deltas.Count == 1 &&
                    m.Deltas[0].IsDestroyed),
                Arg.Is(ChannelType.ReliableOrdered));
        }
    }
}
