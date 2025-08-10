using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Physics;
using NSubstitute;
using Xunit;
using Shared.Prediction;
using Shared.Replication;

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

        [Fact]
        public void Update_PredictedComponentWithInitialValue_SendsOnlyOnCreate()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent>
                { Mode = ReplicationMode.InitialValue };
            entity.AddComponent(pred);

            // Act & Assert (Create)
            _system.Update(_registry, 1, 0);
            _messageSender.Received(1).BroadcastMessage(
                Arg.Any<MessageType>(),
                Arg.Is<WorldDeltaMessage>(m => m.Deltas.Any(d => d.AddedOrModifiedComponents.Contains(pred) && d.IsNew)),
                Arg.Any<ChannelType>());

            _messageSender.ClearReceivedCalls();

            // Act (Modify Tick)
            entity.AddOrReplaceComponent(pred); // This marks it as modified for the next delta
            _system.Update(_registry, 2, 0);

            // Assert (Modify Tick) - No message should be sent for an InitialValue component
            _messageSender.DidNotReceive().BroadcastMessage(Arg.Any<MessageType>(), Arg.Any<WorldDeltaMessage>(), Arg.Any<ChannelType>());
        }

        [Fact]
        public void Update_PredictedComponentWithSomeTicks_SendsPeriodically()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent>
            {
                Mode = ReplicationMode.SomeTicks,
                ReplicationTickRate = 3
            };
            entity.AddComponent(pred);
            _system.Update(_registry, 1, 0); // Consume the "create" delta and clear internal state
            _messageSender.ClearReceivedCalls();

            // Act & Assert (Tick 2 - Modify, No Send)
            entity.AddOrReplaceComponent(pred);
            _system.Update(_registry, 2, 0);
            _messageSender.DidNotReceive().BroadcastMessage(Arg.Any<MessageType>(), Arg.Any<WorldDeltaMessage>(), Arg.Any<ChannelType>());

            // Act & Assert (Tick 4 - Modify, Send)
            entity.AddOrReplaceComponent(pred);
            _system.Update(_registry, 4, 0);
            _messageSender.Received(1).BroadcastMessage(
                Arg.Any<MessageType>(),
                Arg.Is<WorldDeltaMessage>(m => m.Deltas.Any(d => d.AddedOrModifiedComponents.Contains(pred))),
                Arg.Any<ChannelType>());
            Assert.Equal(4u, pred.LastSentAtTick);
        }

        [Fact]
        public void Update_PredictedComponentWithEveryTick_SendsEveryTime()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.EveryTick };
            entity.AddComponent(pred);
            _system.Update(_registry, 1, 0); // Consume the "create" delta
            _messageSender.ClearReceivedCalls();

            // Act & Assert (Tick 2)
            entity.AddOrReplaceComponent(pred);
            _system.Update(_registry, 2, 0);
            _messageSender.Received(1).BroadcastMessage(Arg.Any<MessageType>(), Arg.Any<WorldDeltaMessage>(), Arg.Any<ChannelType>());

            // Act & Assert (Tick 3)
            _messageSender.ClearReceivedCalls();
            entity.AddOrReplaceComponent(pred);
            _system.Update(_registry, 3, 0);
            _messageSender.Received(1).BroadcastMessage(Arg.Any<MessageType>(), Arg.Any<WorldDeltaMessage>(), Arg.Any<ChannelType>());
        }

        [Fact]
        public void Update_WhenPredictedCounterpartModified_DoesNotSendLocalComponent()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.EveryTick };
            var local = new PositionComponent();
            entity.AddComponent(pred);
            entity.AddComponent(local);
            _system.Update(_registry, 1, 0); // Clear create
            _messageSender.ClearReceivedCalls();

            // Act
            entity.AddOrReplaceComponent(local); // Modify the local, which should be ignored
            _system.Update(_registry, 2, 0);

            // Assert
            _messageSender.DidNotReceive().BroadcastMessage(Arg.Any<MessageType>(), Arg.Any<WorldDeltaMessage>(), Arg.Any<ChannelType>());
        }

        [Fact]
        public void Update_OnCreate_SendsBothPredictedAndLocalComponent()
        {
            // Arrange
            var entity = _registry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent>
                { Mode = ReplicationMode.InitialValue };
            var local = new PositionComponent();
            entity.AddComponent(pred);
            entity.AddComponent(local);

            // Act
            _system.Update(_registry, 1, 0);

            // Assert
            _messageSender.Received().BroadcastMessage(
                Arg.Any<MessageType>(),
                Arg.Is<WorldDeltaMessage>(m => m.Deltas.Any(d =>
                    d.AddedOrModifiedComponents.Contains(pred) &&
                    d.AddedOrModifiedComponents.Contains(local))),
                Arg.Any<ChannelType>());
        }
    }
}