using System.Numerics;
using NSubstitute;
using Server.Player;
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
        private readonly IMessageReceiver _messageReceiver = Substitute.For<IMessageReceiver>();
        private readonly ILogger _logger = Substitute.For<ILogger>();
        private readonly ITickSync _tickSync = Substitute.For<ITickSync>();

        [Fact]
        public void HandlePlayerShot_ShouldSpawnProjectile_WhenValidShotReceived()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(10U);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            var shotMessage = new PlayerShotMessage
            {
                Tick = 10,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, shotMessage);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Single(projectiles);

            var projectile = projectiles.First();
            Assert.True(projectile.Has<ReplicatedTagComponent>());
            Assert.True(projectile.Has<DamageApplyingComponent>());
            Assert.True(projectile.Has<SelfDestroyingComponent>());
            Assert.True(projectile.Has<SpawnAuthorityComponent>());

            var spawnAuthority = projectile.GetRequired<SpawnAuthorityComponent>();
            Assert.Equal(peerId, spawnAuthority.SpawnedByPeerId);
            Assert.Equal(shotMessage.Tick, spawnAuthority.SpawnTick);
        }

        [Fact]
        public void HandlePlayerShot_ShouldBlockShot_WhenCooldownNotExpired()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(20U);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            // First shot - should succeed
            var firstShot = new PlayerShotMessage
            {
                Tick = 20,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };
            handler.HandlePlayerShot(peerId, firstShot);

            // Second shot within cooldown - should be blocked
            var secondShot = new PlayerShotMessage
            {
                Tick = 25, // Only 5 ticks later, but cooldown is 15 ticks
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, secondShot);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Single(projectiles); // Only the first shot should have created a projectile

            // Verify warning was logged
            _logger.Received().Warn(Arg.Is<string>(s => s.Contains("blocked by server cooldown")),
                Arg.Any<object[]>());
        }

        [Fact]
        public void HandlePlayerShot_ShouldAllowShot_WhenCooldownExpired()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(20U);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            // First shot
            var firstShot = new PlayerShotMessage
            {
                Tick = 20,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };
            handler.HandlePlayerShot(peerId, firstShot);

            // Advance server tick for cooldown expiry
            _tickSync.ServerTick.Returns(36U);

            var secondShot = new PlayerShotMessage
            {
                Tick = 36, // 16 ticks later, cooldown is 15 ticks so this should work
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, secondShot);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Equal(2, projectiles.Count); // Both shots should have created projectiles
        }

        [Fact]
        public void HandlePlayerShot_ShouldTrackCooldownPerPeer()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId1 = 42;
            var peerId2 = 43;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(20U);

            // Create player entities
            var player1 = _registry.CreateEntity();
            player1.AddComponent(new PeerComponent { PeerId = peerId1 });
            player1.AddComponent(new PlayerTagComponent());
            player1.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            player1.AddComponent(new RotationComponent());

            var player2 = _registry.CreateEntity();
            player2.AddComponent(new PeerComponent { PeerId = peerId2 });
            player2.AddComponent(new PlayerTagComponent());
            player2.AddComponent(new PositionComponent { X = 4, Y = 5, Z = 6 });
            player2.AddComponent(new RotationComponent());

            // First player shoots
            var shot1 = new PlayerShotMessage
            {
                Tick = 20,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };
            handler.HandlePlayerShot(peerId1, shot1);

            // Second player shoots immediately after (different peer, should work)
            var shot2 = new PlayerShotMessage
            {
                Tick = 21,
                Direction = Vector3.UnitX,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId2, shot2);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Equal(2, projectiles.Count); // Both shots should succeed since they're different peers
        }

        [Fact]
        public void HandlePlayerShot_ShouldNotSpawnProjectile_WhenPlayerEntityNotFound()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(10U);

            var shotMsg = new PlayerShotMessage
            {
                Tick = 10,
                Direction = Vector3.UnitX,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act (no player entity exists for this peer ID)
            handler.HandlePlayerShot(999, shotMsg);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Empty(projectiles);
        }

        [Fact]
        public void HandlePlayerShot_ShouldNotSpawnProjectile_WhenDirectionNotNormalized()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(10U);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            var shotMsg = new PlayerShotMessage
            {
                Tick = 10,
                Direction = new Vector3(10, 0, 0), // Not normalized
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, shotMsg);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Empty(projectiles);
        }

        [Theory]
        [InlineData(100U, 10U)] // Too far ahead
        [InlineData(60U, 100U)] // Too far behind 
        public void HandlePlayerShot_ShouldNotSpawnProjectile_WhenTickIsOutOfSync(uint shotTick, uint serverTick)
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(serverTick);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            var shotMsg = new PlayerShotMessage
            {
                Tick = shotTick,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, shotMsg);

            // Assert
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Empty(projectiles);
        }

        [Fact]
        public void OnPeerDisconnected_ShouldCleanupCooldownTracking()
        {
            // Arrange
            var handler = new PlayerShotHandler(_registry, _messageReceiver, _tickSync, _logger);
            var peerId = 42;

            // Set up server tick via tickSync mock
            _tickSync.ServerTick.Returns(10U);

            // Create player entity
            var playerEntity = _registry.CreateEntity();
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            playerEntity.AddComponent(new PlayerTagComponent());
            playerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            playerEntity.AddComponent(new RotationComponent());

            // Fire a shot to establish cooldown tracking
            var shotMsg = new PlayerShotMessage
            {
                Tick = 10,
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };
            handler.HandlePlayerShot(peerId, shotMsg);

            // Act
            handler.OnPeerDisconnected(peerId);

            // Recreate player after disconnect and verify cooldown was cleared
            var newPlayerEntity = _registry.CreateEntity();
            newPlayerEntity.AddComponent(new PeerComponent { PeerId = peerId });
            newPlayerEntity.AddComponent(new PlayerTagComponent());
            newPlayerEntity.AddComponent(new PositionComponent { X = 1, Y = 2, Z = 3 });
            newPlayerEntity.AddComponent(new RotationComponent());

            var secondShot = new PlayerShotMessage
            {
                Tick = 11, // Immediately after, but cooldown should be cleared
                Direction = Vector3.UnitZ,
                PredictedProjectileId = Guid.NewGuid()
            };

            // Act
            handler.HandlePlayerShot(peerId, secondShot);

            // Assert - should have 2 projectiles (cooldown was cleared)
            var projectiles = _registry.GetAll().Where(e => e.Has<ProjectileTagComponent>()).ToList();
            Assert.Equal(2, projectiles.Count);
        }
    }
}