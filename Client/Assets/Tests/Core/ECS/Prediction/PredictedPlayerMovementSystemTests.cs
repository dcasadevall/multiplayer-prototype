using System.Numerics;
using Core.ECS.Prediction;
using Core.Input;
using NSubstitute;
using NUnit.Framework;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using Shared.Networking.Messages;
using ILogger = Shared.Logging.ILogger;

namespace Tests.Core.ECS.Prediction
{
    public class PredictedPlayerMovementSystemTests
    {
        private IInputListener _inputListener;
        private IClientConnection _clientConnection;
        private TickSync _tickSync;
        private ILogger _logger;
        private EntityRegistry _registry;
        private Entity _playerEntity;
        private PredictedPlayerMovementSystem _system;
        private IMessageSender _messageSender;

        [SetUp]
        public void Setup()
        {
            _inputListener = Substitute.For<IInputListener>();
            _clientConnection = Substitute.For<IClientConnection>();
            _logger = Substitute.For<ILogger>();
            _tickSync = new TickSync();
            _registry = new EntityRegistry();
            _messageSender = Substitute.For<IMessageSender>();

            _clientConnection.AssignedPeerId.Returns(1);

            _playerEntity = _registry.CreateEntity();
            _playerEntity.AddComponent(new PeerComponent { PeerId = 1 });
            _playerEntity.AddComponent(new PlayerTagComponent());
            _playerEntity.AddComponent(new PositionComponent { Value = Vector3.Zero });
            _playerEntity.AddComponent(new VelocityComponent { Value = Vector3.Zero });
            _playerEntity.AddComponent(new PredictedComponent<PositionComponent>());
            _playerEntity.AddComponent(new PredictedComponent<VelocityComponent>());

            _system = new PredictedPlayerMovementSystem(
                _clientConnection,
                _messageSender,
                _inputListener,
                _tickSync,
                _logger
            );
        }

        [Test]
        public void Update_InputProvided_PredictsMovement()
        {
            // Simulate input for tick 1
            var input = new Vector2(1, 0);
            _inputListener.TryGetMovementAtTick(1, out Arg.Any<Vector2>())
                .Returns(x => { x[1] = input; return true; });

            _tickSync.ClientTick = 1;
            _system.Update(_registry, 1, 0.016f);

            var pos = _playerEntity.GetRequired<PositionComponent>().Value;
            Assert.AreNotEqual(Vector3.Zero, pos);
        }

        [Test]
        public void Update_ServerStateReceived_ReconcilesWithServerState()
        {
            // Simulate input and prediction
            var input = new Vector2(1, 0);
            _inputListener.TryGetMovementAtTick(1, out Arg.Any<Vector2>())
                .Returns(x => { x[1] = input; return true; });

            _tickSync.ClientTick = 1;
            _system.Update(_registry, 1, 0.016f);

            // Simulate server sending authoritative position for tick 1
            var predictedPos = _playerEntity.GetRequired<PredictedComponent<PositionComponent>>();
            predictedPos.ServerValue = new PositionComponent { Value = new Vector3(10, 0, 0) };
            var predictedVel = _playerEntity.GetRequired<PredictedComponent<VelocityComponent>>();
            predictedVel.ServerValue = new VelocityComponent { Value = Vector3.Zero };

            _tickSync.ServerTick = 1;
            _system.Update(_registry, 2, 0.016f);

            // After reconciliation, error should be applied
            var pos = _playerEntity.GetRequired<PositionComponent>().Value;
            Assert.AreNotEqual(new Vector3(10, 0, 0), pos); // Should be smoothed, not snapped
        }

        [Test]
        public void Update_MoveInputChanged_SendsMovementToServer()
        {
            var input1 = new Vector2(1, 0);
            _inputListener.TryGetMovementAtTick(1, out Arg.Any<Vector2>())
                .Returns(x => { x[1] = input1; return true; });

            _tickSync.ClientTick = 1;
            _system.Update(_registry, 1, 0.016f);

            // Should send movement message to server
            _messageSender.Received(1).SendMessageToServer(
                Arg.Is(MessageType.PlayerMovement),
                Arg.Is<PlayerMovementMessage>(msg =>
                    msg.ClientTick == 1 && msg.MoveDirection == input1)
            );
        }

        [Test]
        public void Update_MoveInputUnchanged_DoesNotSendMovementToServer()
        {
            var input1 = new Vector2(1, 0);
            _inputListener.TryGetMovementAtTick(1, out Arg.Any<Vector2>())
                .Returns(x => { x[1] = input1; return true; });

            _tickSync.ClientTick = 1;
            _system.Update(_registry, 1, 0.016f);

            // Simulate same input for next tick
            _inputListener.TryGetMovementAtTick(2, out Arg.Any<Vector2>())
                .Returns(x => { x[1] = input1; return true; });

            _tickSync.ClientTick = 2;
            _system.Update(_registry, 2, 0.016f);

            // Should only send once, not twice
            _messageSender.Received(1).SendMessageToServer(
                Arg.Any<MessageType>(),
                Arg.Any<PlayerMovementMessage>()
            );
        }
    }
}
