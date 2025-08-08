using System.Numerics;
using Core.Physics;
using NSubstitute;
using NUnit.Framework;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Networking;
using Shared.Physics;

namespace Tests.Core.ECS.Prediction
{
    public class VelocityPredictionSystemTests
    {
        private IClientConnection _clientConnection;
        private TickSync _tickSync;
        private EntityRegistry _registry;
        private Entity _remoteEntity;
        private VelocityPredictionSystem _system;

        [SetUp]
        public void Setup()
        {
            _clientConnection = Substitute.For<IClientConnection>();
            _tickSync = new TickSync();
            _registry = new EntityRegistry();

            _clientConnection.AssignedPeerId.Returns(1);

            _remoteEntity = _registry.CreateEntity();
            _remoteEntity.AddComponent(new PeerComponent { PeerId = 2 });
            _remoteEntity.AddComponent(new PositionComponent { Value = Vector3.Zero });
            _remoteEntity.AddComponent(new VelocityComponent { Value = Vector3.Zero });
            _remoteEntity.AddComponent(new PredictedComponent<PositionComponent>());
            _remoteEntity.AddComponent(new PredictedComponent<VelocityComponent>());

            _system = new VelocityPredictionSystem(
                _clientConnection,
                _tickSync
            );
        }

        [Test]
        public void Update_ServerVelocityAndPositionProvided_InterpolatesPosition()
        {
            // Simulate server sending velocity and position
            var predictedPos = _remoteEntity.GetRequired<PredictedComponent<PositionComponent>>();
            predictedPos.ServerValue = new PositionComponent { Value = new Vector3(5, 0, 0) };
            var predictedVel = _remoteEntity.GetRequired<PredictedComponent<VelocityComponent>>();
            predictedVel.ServerValue = new VelocityComponent { Value = new Vector3(1, 0, 0) };

            _tickSync.ClientTick = 2;
            _tickSync.ServerTick = 1;
            _tickSync.SmoothedTick = 2;

            _system.Update(_registry, 2, 0.016f);

            var pos = _remoteEntity.GetRequired<PositionComponent>().Value;
            Assert.Greater(pos.X, 5); // Should be interpolated/extrapolated forward
        }

        [Test]
        public void Update_PositionFarFromServer_SnapsToServerPosition()
        {
            // Set initial position far from server position
            _remoteEntity.GetRequired<PositionComponent>().Value = new Vector3(100, 0, 0);

            var predictedPos = _remoteEntity.GetRequired<PredictedComponent<PositionComponent>>();
            predictedPos.ServerValue = new PositionComponent { Value = new Vector3(5, 0, 0) };
            var predictedVel = _remoteEntity.GetRequired<PredictedComponent<VelocityComponent>>();
            predictedVel.ServerValue = new VelocityComponent { Value = Vector3.Zero };

            _tickSync.ClientTick = 2;
            _tickSync.ServerTick = 1;
            _tickSync.SmoothedTick = 2;

            _system.Update(_registry, 2, 0.016f);

            var pos = _remoteEntity.GetRequired<PositionComponent>().Value;
            Assert.AreEqual(5, pos.X); // Should snap to server/extrapolated position
        }

        [Test]
        public void Update_LocalPlayerEntity_DoesNotPredictVelocity()
        {
            // Create a local player entity (peer id matches client)
            var localEntity = _registry.CreateEntity();
            localEntity.AddComponent(new PeerComponent { PeerId = 1 });
            localEntity.AddComponent(new PlayerTagComponent());
            localEntity.AddComponent(new PositionComponent { Value = Vector3.Zero });
            localEntity.AddComponent(new VelocityComponent { Value = new Vector3(10, 0, 0) });
            localEntity.AddComponent(new PredictedComponent<PositionComponent>());
            localEntity.AddComponent(new PredictedComponent<VelocityComponent>());

            // Provide server values, which should be ignored for local player
            var predictedPos = localEntity.GetRequired<PredictedComponent<PositionComponent>>();
            predictedPos.ServerValue = new PositionComponent { Value = new Vector3(50, 0, 0) };
            var predictedVel = localEntity.GetRequired<PredictedComponent<VelocityComponent>>();
            predictedVel.ServerValue = new VelocityComponent { Value = new Vector3(100, 0, 0) };

            _tickSync.ClientTick = 2;
            _tickSync.ServerTick = 1;
            _tickSync.SmoothedTick = 2;

            // Save original position and velocity
            var originalPosition = localEntity.GetRequired<PositionComponent>().Value;
            var originalVelocity = localEntity.GetRequired<VelocityComponent>().Value;

            _system.Update(_registry, 2, 0.016f);

            // Assert that local player position and velocity were not changed by velocity prediction system
            Assert.AreEqual(originalPosition, localEntity.GetRequired<PositionComponent>().Value);
            Assert.AreEqual(originalVelocity, localEntity.GetRequired<VelocityComponent>().Value);
        }
    }
}
