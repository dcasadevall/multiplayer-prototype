using NSubstitute;
using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Physics;
using Shared.Prediction;
using Shared.Replication;
using Xunit;

namespace SharedUnitTests.Replication
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
            var tickSync = new Shared.ECS.TickSync.TickSync();
            _serverSystem = new ServerReplicationSystem(_serverRegistry, tickSync, messageSender, serverMessageFactory, logger);
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
            Assert.Equal(serverPosition.Value, clientPredicted.ServerValue.Value);
        }

        [Fact]
        public void ModifyPredictedEntity_ServerToClient_UpdatesServerValueOnly()
        {
            // --- SERVER SIDE ---
            // Arrange: Create an entity and run replication once to establish it on the client.
            var serverEntity = _serverRegistry.CreateEntity();
            var serverPosition = new PositionComponent { Value = new(1, 1, 1) };
            var serverPredicted = new PredictedComponent<PositionComponent>
            {
                Mode = ReplicationMode.EveryTick,
                ServerValue = serverPosition
            };
            serverEntity.AddComponent(serverPredicted);
            serverEntity.AddComponent(serverPosition);
            _serverSystem.Update(_serverRegistry, 1, 0);
            _clientSystem.Update(_clientRegistry, 1, 0);

            // Arrange: Modify the server's component and the client's local predicted component
            if (!_clientRegistry.TryGet(serverEntity.Id, out var clientEntity))
            {
                throw new InvalidOperationException("Client entity not found after initial replication.");
            }

            clientEntity.GetRequired<PositionComponent>().Value = new(2, 2, 2); // Client predicts

            var newServerPosition = new PositionComponent { Value = new(3, 3, 3) };
            serverPredicted.ServerValue = newServerPosition;
            serverEntity.AddOrReplaceComponent(serverPredicted); // Mark for modification

            // Act: Server sends the update, client consumes it
            _serverSystem.Update(_serverRegistry, 2, 0);
            _clientSystem.Update(_clientRegistry, 2, 0);

            // Assert: The client's predicted wrapper has the new server value, but the local component is unchanged.
            var clientPredicted = clientEntity.GetRequired<PredictedComponent<PositionComponent>>();
            var clientPosition = clientEntity.GetRequired<PositionComponent>();

            Assert.NotNull(clientPredicted.ServerValue);
            Assert.Equal(newServerPosition.Value, clientPredicted.ServerValue.Value);
            Assert.Equal(new(2, 2, 2), clientPosition.Value); // Unchanged by replication
        }

        [Fact]
        public void ModifyLocalCounterpart_OnServer_ReplicatesPredictedComponent()
        {
            // --- SERVER SIDE ---
            // Arrange: Create a predicted entity and establish it on the client.
            var serverEntity = _serverRegistry.CreateEntity();
            var serverPosition = new PositionComponent { Value = new(1, 1, 1) };
            var serverPredicted = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.EveryTick };
            serverEntity.AddComponent(serverPredicted);
            serverEntity.AddComponent(serverPosition);
            _serverSystem.Update(_serverRegistry, 1, 0);
            _clientSystem.Update(_clientRegistry, 1, 0);

            // Arrange: Modify ONLY the local component on the server.
            serverPosition.Value = new(3, 3, 3);
            serverEntity.AddOrReplaceComponent(serverPosition);

            // Act: Server sends the update, client consumes it.
            _serverSystem.Update(_serverRegistry, 2, 0);
            _clientSystem.Update(_clientRegistry, 2, 0);

            // Assert: The client's predicted wrapper received the update.
            var clientEntity = _clientRegistry.Get(serverEntity.Id);
            var clientPredicted = clientEntity.GetRequired<PredictedComponent<PositionComponent>>();
            Assert.NotNull(clientPredicted.ServerValue);
            Assert.Equal(new(3, 3, 3), clientPredicted.ServerValue.Value);
        }

        [Fact]
        public void Update_WhenPredictedCounterpartModified_SendsPredictedComponentButNotLocal()
        {
            // Arrange
            var entity = _serverRegistry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.EveryTick };
            var local = new PositionComponent();
            entity.AddComponent(pred);
            entity.AddComponent(local);
            _serverSystem.Update(_serverRegistry, 1, 0); // Clear create
            _capturedDeltaMessage = null; // Clear captured message

            // Act
            entity.AddOrReplaceComponent(local); // Modify the local
            _serverSystem.Update(_serverRegistry, 2, 0);

            // Assert
            Assert.NotNull(_capturedDeltaMessage);
            Assert.Contains(_capturedDeltaMessage.Deltas, d =>
                    d.AddedOrModifiedComponents.Contains(pred) && // Wrapper IS sent
                    !d.AddedOrModifiedComponents.Contains(local) // Local IS NOT sent
            );
        }

        [Fact]
        public void Update_WhenPredictedCounterpartModified_SkipsInitialOnly()
        {
            // Arrange
            var entity = _serverRegistry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.InitialValue };
            var local = new PositionComponent();
            entity.AddComponent(pred);
            entity.AddComponent(local);
            _serverSystem.Update(_serverRegistry, 1, 0); // Clear create
            _capturedDeltaMessage = null; // Clear captured message

            // Act
            entity.AddOrReplaceComponent(local);
            _serverSystem.Update(_serverRegistry, 2, 0);

            // Assert
            Assert.Null(_capturedDeltaMessage);
        }

        [Fact]
        public void Update_WhenPredictedCounterpartModified_SkipsSomeTicksOffRate()
        {
            // Arrange
            var entity = _serverRegistry.CreateEntity();
            var pred = new PredictedComponent<PositionComponent> { Mode = ReplicationMode.SomeTicks, ReplicationTickRate = 5 };
            var local = new PositionComponent();
            entity.AddComponent(pred);
            entity.AddComponent(local);
            _serverSystem.Update(_serverRegistry, 1, 0); // Clear create and set LastSentAtTick to 1
            _capturedDeltaMessage = null; // Clear captured message

            // Act
            entity.AddOrReplaceComponent(local);
            _serverSystem.Update(_serverRegistry, 3, 0); // Tick 3 is off-rate (1 + 5 = 6)

            // Assert
            Assert.Null(_capturedDeltaMessage);
        }
    }
}