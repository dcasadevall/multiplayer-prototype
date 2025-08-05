using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking;

namespace Adapters.Player
{
    public class RemotePlayerMovementPredictionSystem : ISystem
    {
        private readonly TickSync _tickSync;
        private readonly ILogger _logger;
        private readonly int _localPeerId;
        private const float MaxCorrectionDistance = 2.0f;

        public RemotePlayerMovementPredictionSystem(IClientConnection connection, TickSync tickSync, ILogger logger)
        {
            _tickSync = tickSync;
            _logger = logger;
            _localPeerId = connection.AssignedPeerId;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var remotePlayerEntities = registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PredictedComponent<PositionComponent>>())
                .Where(x => x.GetRequired<PeerComponent>().PeerId != _localPeerId);

            foreach (var entity in remotePlayerEntities)
            {
                var positionComponent = entity.GetRequired<PositionComponent>();
                var velocityComponent = entity.GetRequired<VelocityComponent>();
                if (!entity.TryGet(out PredictedStateComponent predictedState))
                {
                    entity.AddComponent<PredictedStateComponent>();
                    predictedState = entity.GetRequired<PredictedStateComponent>();
                }

                var authoritativePositionComponent = entity.GetRequired<PredictedComponent<PositionComponent>>();
                var authoritativeVelocityComponent = entity.GetRequired<PredictedComponent<VelocityComponent>>();

                if (authoritativePositionComponent.ServerValue == null || authoritativeVelocityComponent.ServerValue == null)
                {
                    _logger.Warn("Remote player entity {0} does not have authoritative position or velocity.", entity.Id);
                    continue;
                }

                var authoritativePosition = authoritativePositionComponent.ServerValue.Value;
                var authoritativeVelocity = authoritativeVelocityComponent.ServerValue.Value;
                var serverTick = _tickSync.ServerTick;

                // Update velocity from server
                velocityComponent.Value = authoritativeVelocity;

                // First time receiving authoritative data or fresh update from server
                if (predictedState.LastServerTick != serverTick)
                {
                    predictedState.LastServerTick = serverTick;
                    predictedState.PredictedPosition = authoritativePosition;

                    // If current predicted position is too far, snap or correct
                    float dist = Vector3.Distance(positionComponent.Value, authoritativePosition);
                    if (dist > MaxCorrectionDistance)
                    {
                        positionComponent.Value = authoritativePosition;
                    }
                }

                // Predict forward every tick based on last known velocity
                predictedState.PredictedPosition += velocityComponent.Value * deltaTime;

                // Smooth the visual position toward the predicted one
                positionComponent.Value = Vector3.Lerp(positionComponent.Value, predictedState.PredictedPosition, 0.5f);
            }
        }
    }
}
