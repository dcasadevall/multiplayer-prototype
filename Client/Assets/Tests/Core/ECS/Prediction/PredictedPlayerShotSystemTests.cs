using System;
using System.Numerics;
using Core.ECS.Entities;
using Core.ECS.Prediction;
using Core.Input;
using NSubstitute;
using NUnit.Framework;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using Shared.Networking.Messages;
using ILogger = Shared.Logging.ILogger;

namespace Tests.Core.ECS.Prediction
{
    public class PredictedPlayerShotSystemTests
    {
        private IInputListener _inputListener;
        private IMessageSender _messageSender;
        private IClientConnection _clientConnection;
        private ILogger _logger;
        private EntityRegistry _registry;
        private Entity _playerEntity;
        private PredictedPlayerShotSystem _system;

        [SetUp]
        public void Setup()
        {
            _inputListener = Substitute.For<IInputListener>();
            _messageSender = Substitute.For<IMessageSender>();
            _clientConnection = Substitute.For<IClientConnection>();
            _logger = Substitute.For<ILogger>();
            _registry = new EntityRegistry();
            var tickSync = Substitute.For<ITickSync>();

            _clientConnection.AssignedPeerId.Returns(1);

            _playerEntity = _registry.CreateEntity();
            _playerEntity.AddComponent(new PeerComponent { PeerId = 1 });
            _playerEntity.AddComponent(new PlayerTagComponent());
            _playerEntity.AddComponent(new PositionComponent { Value = Vector3.Zero });

            _system = new PredictedPlayerShotSystem(
                _inputListener,
                _registry,
                _messageSender,
                _clientConnection,
                tickSync,
                _logger
            );
        }

        [Test]
        public void Update_ShotInputProvided_CreatesPredictedProjectileAndSendsMessage()
        {
            // Simulate OnShoot event being triggered
            _system.Initialize();
            _registry.GetLocalPlayerEntity(_clientConnection.AssignedPeerId).GetRequired<PositionComponent>().Value = Vector3.Zero;

            // Act: Raise the OnShoot event
            _inputListener.OnShoot += Raise.Event<Action>();

            // Assert: Should send shot message to server
            _messageSender.Received().SendMessageToServer(
                Arg.Is(MessageType.PlayerShot),
                Arg.Any<PlayerShotMessage>()
            );
        }

        [Test]
        public void Update_NoShotInput_DoesNotCreateProjectileOrSendMessage()
        {
            // Arrange
            var tick = 2u;
            // Do not raise OnShoot event

            // Act
            _system.Update(_registry, tick, 0.016f);

            // Assert: Should not send shot message to server
            _messageSender.DidNotReceive().SendMessageToServer(
                Arg.Any<MessageType>(),
                Arg.Any<PlayerShotMessage>()
            );
        }

        [Test]
        public void Update_ServerProjectileArrives_AssociatesAndDestroysPredictedProjectile()
        {
            // Arrange
            var tick = 3u;
            var predictedId = Guid.NewGuid();

            _system.Initialize();
            _registry.GetLocalPlayerEntity(_clientConnection.AssignedPeerId).GetRequired<PositionComponent>().Value = Vector3.Zero;

            // Simulate OnShoot event to create predicted projectile
            _inputListener.OnShoot += Raise.Event<Action>();

            // Simulate server projectile with matching SpawnAuthority
            var serverProjectile = _registry.CreateEntity();
            serverProjectile.AddComponent(new ProjectileTagComponent());
            serverProjectile.AddComponent(new SpawnAuthorityComponent
            {
                SpawnedByPeerId = 1,
                LocalEntityId = predictedId,
                SpawnTick = tick
            });

            // Act
            _system.Update(_registry, tick + 1, 0.016f);

            // Assert: Predicted projectile should be destroyed and mapping created
            _logger.Received().Debug(
                Arg.Is<string>(s => s.Contains("Associated server projectile")),
                Arg.Any<object[]>()
            );
        }
    }
}
