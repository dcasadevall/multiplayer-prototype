using LiteNetLib.Utils;
using Shared.Input;
using Xunit;

namespace SharedUnitTests.Input
{
    public class PlayerShotMessageTests
    {
        [Fact]
        public void SerializeAndDeserialize_PlayerShotMessage_ReturnsEqualMessage()
        {
            // Arrange
            var originalMessage = new PlayerShotMessage
            {
                Tick = 123,
                PredictedProjectileId = Guid.NewGuid()
            };

            var writer = new NetDataWriter();
            var reader = new NetDataReader();

            // Act
            originalMessage.Serialize(writer);
            reader.SetSource(writer);

            var deserializedMessage = new PlayerShotMessage();
            deserializedMessage.Deserialize(reader);

            // Assert
            Assert.Equal(originalMessage.Tick, deserializedMessage.Tick);
            Assert.Equal(originalMessage.PredictedProjectileId, deserializedMessage.PredictedProjectileId);
        }
    }
}