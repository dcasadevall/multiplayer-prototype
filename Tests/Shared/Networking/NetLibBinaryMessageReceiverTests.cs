using LiteNetLib;
using LiteNetLib.Utils;
using NSubstitute;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Xunit;

namespace SharedUnitTests.Networking
{
    public class NetLibBinaryMessageReceiverTests
    {
        [Fact]
        public void OnNetworkReceiveEvent_WithRegisteredHandler_DeserializesAndCallsHandler()
        {
            // Arrange
            var listener = new EventBasedNetListener();
            var logger = Substitute.For<ILogger>();
            var componentSerializer = Substitute.For<Shared.ECS.Replication.IComponentSerializer>();
            var messageFactory = new MessageFactory(componentSerializer);
            var receiver = new NetLibBinaryMessageReceiver(listener, messageFactory, logger);
            receiver.Initialize();

            PlayerShotMessage receivedMessage = null;
            receiver.RegisterMessageHandler<PlayerShotMessage>("test", (peerId, msg) => receivedMessage = msg);

            var originalMessage = new PlayerShotMessage { Tick = 42, PredictedProjectileId = Guid.NewGuid() };
            var writer = new NetDataWriter();
            writer.Put((byte)MessageType.PlayerShot);
            originalMessage.Serialize(writer);

            // Act
            listener.OnNetworkReceive(null, new EventBasedNetListener(), 0, DeliveryMethod.ReliableOrdered);

            // Assert
            Assert.NotNull(receivedMessage);
            Assert.Equal(originalMessage.Tick, receivedMessage.Tick);
            Assert.Equal(originalMessage.PredictedProjectileId, receivedMessage.PredictedProjectileId);
        }
    }
}