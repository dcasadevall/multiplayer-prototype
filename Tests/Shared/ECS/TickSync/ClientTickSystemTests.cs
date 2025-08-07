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
            connection.PingMs.Returns(60); // 60ms ping
            var tickSync = new Shared.ECS.TickSync.TickSync();
            // Need to initialize TickSync to avoid the IsInitialized override
            tickSync.IsInitialized = true;
            var system = new ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickEntity = registry.CreateEntity();
            tickEntity.AddComponent(new ServerTickComponent { TickNumber = 100 });

            // Act - Test when client is ahead of server
            system.Update(registry, 110, 0.02f);

            // Assert
            Assert.Equal(100U, tickSync.ServerTick);
            // ClientTick is set to tickNumber (110) at start of Update()
            // Since TickSync is initialized, it won't be overridden to ServerTick
            // targetOffset = (int)(60 / 1000.0f / 0.02f) + 2 = (int)(3) + 2 = 5
            // currentOffset = 110 - 100 = 10
            // Since currentOffset (10) <= targetOffset + DriftTolerance (5 + 10 = 15), it's in sweet spot
            // So it should increment by 1: 110 + 1 = 111
            Assert.Equal(111U, tickSync.ClientTick);
        }

        [Fact]
        public void Update_IncrementsClientTick_When_TooCloseToServer()
        {
            // Arrange
            var connection = Substitute.For<IClientConnection>();
            connection.PingMs.Returns(100); // 100ms ping
            var tickSync = new Shared.ECS.TickSync.TickSync();
            // Need to initialize TickSync to avoid the IsInitialized override
            tickSync.IsInitialized = true;
            var system = new ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickEntity = registry.CreateEntity();
            tickEntity.AddComponent(new ServerTickComponent { TickNumber = 100 });

            // Act - Test when client is too close to server
            system.Update(registry, 102, 0.02f);

            // Assert
            Assert.Equal(100U, tickSync.ServerTick);
            // targetOffset = (int)(100 / 1000.0f / 0.02f) + 2 = (int)(5) + 2 = 7
            // currentOffset = 102 - 100 = 2
            // Since currentOffset (2) < targetOffset (7), should speed up by TickSmoothAmount (2)
            // 102 + 2 = 104
            Assert.Equal(104U, tickSync.ClientTick);
        }

        [Fact]
        public void Update_StallsClientTick_When_TooFarAhead()
        {
            // Arrange
            var connection = Substitute.For<IClientConnection>();
            connection.PingMs.Returns(0); // No ping
            var tickSync = new Shared.ECS.TickSync.TickSync();
            // Need to initialize TickSync to avoid the IsInitialized override
            tickSync.IsInitialized = true;
            var system = new ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickEntity = registry.CreateEntity();
            tickEntity.AddComponent(new ServerTickComponent { TickNumber = 100 });

            // Act - Test when client is too far ahead
            system.Update(registry, 120, 0.02f);

            // Assert
            Assert.Equal(100U, tickSync.ServerTick);
            // targetOffset = (int)(0 / 0.02f) + 2 = 2
            // currentOffset = 120 - 100 = 20
            // Since currentOffset (20) > targetOffset + DriftTolerance (2 + 10 = 12), should stall
            // ClientTick remains 120 (no increment)
            Assert.Equal(120U, tickSync.ClientTick);
        }
    }
}