using System.Numerics;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Replication;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Xunit;

namespace ServerUnitTests.Player
{
    public class PlayerShotHandlerTests
    {
        private readonly EntityRegistry _registry = new();
        private readonly IMessageReceiver _messageReceiverMock = Substitute.For<IMessageReceiver>();
        private readonly ILogger _loggerMock = Substitute.For<ILogger>();

        [Fact]
        public void Initialize_ShouldRegisterMessageHandler()
        {
            // Arrange
            var handler = new Server.Player.PlayerShotHandler(_registry, _messageReceiverMock, _loggerMock);

            // Act
            handler.Initialize();

            // Assert
            _messageReceiverMock.Received(1)
                .RegisterMessageHandler(
                    Arg.Any<string>(), Arg.Any<MessageHandler<PlayerShotMessage>>());
        }

        [Fact]
        public void HandlePlayerShot_ShouldSpawnProjectile_WhenShotIsValid()
        {
            // Arrange
            var peerId = 99;
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { Value = new Vector3(1, 2, 3) });

            var serverTickEntity = _registry.CreateEntity();
            serverTickEntity.AddComponent(new ServerTickComponent { TickNumber = 5 });

            var handler = new Server.Player.PlayerShotHandler(_registry, _messageReceiverMock, _loggerMock);

            var shotMsg = new PlayerShotMessage
            {
                Tick = 5,
                FireDirection = Vector2.UnitY,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            var method = handler.GetType().GetMethod("HandlePlayerShotMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(handler, [peerId, shotMsg]);

            // Assert
            var projectile = _registry.GetAll().FirstOrDefault(e => e.Has<ProjectileTagComponent>());
            Assert.NotNull(projectile);
            Assert.True(projectile.Has<ReplicatedTagComponent>());
            Assert.Equal(5U, projectile.GetRequired<SpawnAuthorityComponent>().SpawnTick);
        }

        [Fact]
        public void HandlePlayerShotMessage_ShouldNotSpawnProjectile_WhenPlayerEntityNotFound()
        {
            // Arrange
            var handler = new Server.Player.PlayerShotHandler(_registry, _messageReceiverMock, _loggerMock);
            var shotMsg = new PlayerShotMessage
            {
                Tick = 1,
                FireDirection = Vector2.UnitX,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            var method = handler.GetType().GetMethod("HandlePlayerShotMessage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(handler, [42, shotMsg]);

            // Assert
            var projectile = _registry.GetAll().FirstOrDefault(e => e.Has<ProjectileTagComponent>());
            Assert.Null(projectile);
        }

        [Fact]
        public void ValidateShot_ShouldReturnFalse_WhenTickTooFarInFuture()
        {
            // Arrange
            var handler = new Server.Player.PlayerShotHandler(_registry, _messageReceiverMock, _loggerMock);
            var shotMsg = new PlayerShotMessage
            {
                Tick = 100,
                FireDirection = Vector2.UnitX,
                PredictedProjectileId = Guid.NewGuid()
            };

            var method = handler.GetType().GetMethod("ValidateShot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(handler, [shotMsg, 1u])!;

            Assert.False(result);
        }

        [Fact]
        public void ValidateShot_ShouldReturnFalse_WhenDirectionNotNormalized()
        {
            // Arrange
            var handler = new Server.Player.PlayerShotHandler(_registry, _messageReceiverMock, _loggerMock);
            var shotMsg = new PlayerShotMessage
            {
                Tick = 1,
                FireDirection = new Vector2(10, 0),
                PredictedProjectileId = Guid.NewGuid()
            };

            var method = handler.GetType().GetMethod("ValidateShot",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (bool)method!.Invoke(handler, [shotMsg, 1u])!;

            Assert.False(result);
        }
    }
}