using NSubstitute;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.Networking;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class ClientReplicationSystemTests
    {
        [Fact]
        public void Update_WhenInitialized_ConsumesInitialWorldSnapshot()
        {
            // Arrange
            var registry = new EntityRegistry();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();

            var entityId = Guid.NewGuid();
            var initialSnapshot = new WorldDeltaMessage(Substitute.For<IComponentSerializer>())
            {
                Deltas =
                [
                    new EntityDelta
                    {
                        EntityId = entityId,
                        IsNew = true
                    }
                ]
            };
            connection.InitialWorldSnapshot.Returns(initialSnapshot);

            var system = new ClientReplicationSystem(messageReceiver, connection);

            // Act
            system.Update(registry, 0, 0);

            // Assert
            Assert.True(registry.TryGet(new EntityId(entityId), out _));
        }

        [Fact]
        public void Update_WhenDeltaMessageIsReceived_ConsumesTheMessage()
        {
            // Arrange
            var registry = new EntityRegistry();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();
            connection.InitialWorldSnapshot.Returns(new WorldDeltaMessage(Substitute.For<IComponentSerializer>())
            {
                Deltas = new List<EntityDelta>()
            });

            // Capture the message handler that the system registers in its constructor.
            MessageHandler<WorldDeltaMessage> messageHandler = null!;
            messageReceiver.RegisterMessageHandler(Arg.Any<string>(),
                Arg.Do<MessageHandler<WorldDeltaMessage>>(handler => messageHandler = handler));

            var system = new ClientReplicationSystem(messageReceiver, connection);

            var entityId = Guid.NewGuid();
            var deltaMessage = new WorldDeltaMessage(Substitute.For<IComponentSerializer>())
            {
                Deltas =
                [
                    new EntityDelta()
                    {
                        EntityId = entityId,
                        IsNew = true
                    }
                ]
            };

            // Act
            // Manually invoke the captured handler to simulate a message being received.
            Assert.NotNull(messageHandler);
            messageHandler.Invoke(0, deltaMessage);

            // Now, run the system's update, which should process the enqueued message.
            system.Update(registry, 1, 0);

            // Assert
            Assert.True(registry.TryGet(new EntityId(entityId), out _));
        }

        [Fact]
        public void Constructor_ThrowsException_WhenInitialWorldSnapshotIsNull()
        {
            // Arrange
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();
            connection.InitialWorldSnapshot.Returns((WorldDeltaMessage)null!);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ClientReplicationSystem(messageReceiver, connection));
        }
    }
}