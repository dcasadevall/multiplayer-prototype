using System.Linq;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.TickSync;
using Shared.Networking;
using Xunit;

namespace SharedUnitTests.ECS.TickSync
{
    public class ClientTickSystemTests
    {
        [Fact]
        public void Update_SetsServerTick_ToServerTickComponentValue_And_Corrects_ClientTick()
        {
            // Arrange
            var connection = Substitute.For<IClientConnection>();
            connection.PingMs.Returns(0); // No ping for simplicity
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var system = new ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickEntity = registry.CreateEntity();
            tickEntity.AddComponent(new ServerTickComponent { TickNumber = 42 });

            // Act
            system.Update(registry, 99, 0.02f);

            // Assert
            Assert.Equal(42U, tickSync.ServerTick);
            // The logic in CorrectForDrift will increment ClientTick by 2 (since offset < targetOffset)
            // targetOffset = (int)(0 / 0.02f) + 2 = 2
            // currentOffset = 99 - 42 = 57
            // Since currentOffset > targetOffset + 10, it should stall (no increment)
            // So ClientTick remains 99
            Assert.Equal(99U, tickSync.ClientTick);

            // Now test the "too close" case
            system.Update(registry, 43, 0.02f); // currentOffset = 1, targetOffset = 2
            // Should increment by 2
            Assert.Equal(45U, tickSync.ClientTick);

            // Now test the "sweet spot" case
            system.Update(registry, 55, 0.02f); // currentOffset = 13, targetOffset = 2
            // Should increment by 1 (normal operation)
            Assert.Equal(56U, tickSync.ClientTick);
        }
    }
}