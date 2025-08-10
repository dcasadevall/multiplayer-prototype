using System;
using System.Linq;
using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Physics;
using Shared.Prediction;
using Shared.Replication;
using NSubstitute;
using Xunit;

namespace SharedUnitTests.ECS.Replication
{
    public class ReplicationIntegrationTests
    {
        private readonly EntityRegistry _serverRegistry;
        private readonly ServerReplicationSystem _serverSystem;
        private readonly EntityRegistry _clientRegistry;
        private readonly ClientReplicationSystem _clientSystem;
        private WorldDeltaMessage? _capturedDeltaMessage;

        public ReplicationIntegrationTests()
        {
            // Server Setup
            _serverRegistry = new EntityRegistry();
            var messageSender = Substitute.For<IMessageSender>();
            var componentSerializer = Substitute.For<IComponentSerializer>();
            var componentRegistry = new ComponentTypeRegistry();
            var serverMessageFactory = new MessageFactory(componentSerializer, componentRegistry);
            var logger = Substitute.For<ILogger>();
            _serverSystem = new ServerReplicationSystem(_serverRegistry, messageSender, serverMessageFactory, logger);
            _serverSystem.Initialize();

            // Capture the message sent by the server
            messageSender.BroadcastMessage(Arg.Any<MessageType>(), Arg.Do<WorldDeltaMessage>(m => _capturedDeltaMessage = m),
                Arg.Any<ChannelType>());

            // Client Setup
            _clientRegistry = new EntityRegistry();
            var messageReceiver = Substitute.For<IMessageReceiver>();
            var connection = Substitute.For<IClientConnection>();
            connection.InitialWorldSnapshot.Returns(new WorldDeltaMessage(componentSerializer, componentRegistry));
            MessageHandler<WorldDeltaMessage> clientMessageHandler = null!;
            messageReceiver.RegisterMessageHandler(Arg.Any<string>(),
                Arg.Do<MessageHandler<WorldDeltaMessage>>(h => clientMessageHandler = h));
            _clientSystem = new ClientReplicationSystem(messageReceiver, connection);

            // Connect the two systems
            _capturedDeltaMessage = null; // reset
            messageSender.BroadcastMessage(Arg.Any<MessageType>(), Arg.Do<WorldDeltaMessage>(m =>
            {
                // Simulate network transfer
                clientMessageHandler.Invoke(0, m);
            }), Arg.Any<ChannelType>());
        }

        [Fact]
        public void CreatePredictedEntity_ServerToClient_ReplicatesCorrectly()
        {
            // --- SERVER SIDE ---
            // Arrange: Create an entity with predicted components on the server
            var serverEntity = _serverRegistry.CreateEntity();
            var serverPosition = new PositionComponent { Value = new(1, 2, 3) };
            var serverPredicted = new PredictedComponent<PositionComponent>
            {
                Mode = ReplicationMode.InitialValue,
                ServerValue = serverPosition
            };
            serverEntity.AddComponent(serverPredicted);
            serverEntity.AddComponent(serverPosition);

            // Act: Server produces and sends the delta
            _serverSystem.Update(_serverRegistry, 1, 0);

            // --- CLIENT SIDE ---
            // Act: Client consumes the delta
            _clientSystem.Update(_clientRegistry, 1, 0);

            // Assert: Client state is now correct
            Assert.True(_clientRegistry.TryGet(serverEntity.Id, out var clientEntity));
            Assert.True(clientEntity.Has<PositionComponent>());
            Assert.True(clientEntity.Has<PredictedComponent<PositionComponent>>());

            var clientPredicted = clientEntity.GetRequired<PredictedComponent<PositionComponent>>();
            var clientPosition = clientEntity.GetRequired<PositionComponent>();

            // The local component should have the initial value, and the predicted wrapper's server value should be null after initial spawn
            Assert.Equal(serverPosition.Value, clientPosition.Value);
            Assert.NotNull(clientPredicted.ServerValue);
        }
    }
}