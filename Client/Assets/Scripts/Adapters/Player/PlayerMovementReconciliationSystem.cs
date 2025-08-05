using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Networking;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;
using Vector3 = System.Numerics.Vector3;

namespace Adapters.Player
{
    public class PlayerMovementReconciliationSystem : ISystem
    {
        private readonly PlayerMovementPredictionSystem _prediction;
        private readonly ILogger _logger;
        private readonly TickSync _tickSync;
        private readonly int _localPeerId;

        // Error threshold. If distance is greater than this, we reconcile.
        private const float ReconciliationThreshold = 0.1f;

        public PlayerMovementReconciliationSystem(PlayerMovementPredictionSystem prediction,
            IClientConnection connection,
            ILogger logger,
            TickSync tickSync)
        {
            _prediction = prediction;
            _logger = logger;
            _tickSync = tickSync;
            _localPeerId = connection.AssignedPeerId;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Get all player entities
            var playerEntities = registry.GetAll()
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PeerComponent>());

            playerEntities.ToList().ForEach(entity => PredictAndReconcileEntityMovement(entity, deltaTime));
        }

        private void PredictAndReconcileEntityMovement(Entity entity, float deltaTime)
        {
            var serverTick = _tickSync.ServerTick;
            
            // We can't reconcile if the server tick is 0 or we have no state for it.
            if (serverTick == 0) return;

            // 1. Get the server's authoritative state for its last processed tick.
            var authoritativePosComponent = entity.GetRequired<PredictedComponent<PositionComponent>>();
            var authoritativeVelComponent = entity.GetRequired<PredictedComponent<VelocityComponent>>();
            if (authoritativePosComponent.ServerValue == null || authoritativeVelComponent.ServerValue == null)
            {
                // This can happen if we haven't received a state update from the server yet.
                return;
            }

            var authoritativePosition = authoritativePosComponent.ServerValue.Value;
            var authoritativeVelocity = authoritativeVelComponent.ServerValue.Value;
            
            // This is a remote peer. We predict their movement to make them appear smoother.
            if (entity.GetRequired<PeerComponent>().PeerId != _localPeerId)
            {
                // Get the current client-side position and update velocity to the latest from the server.
                var positionComponent = entity.GetRequired<PositionComponent>();
                entity.AddOrReplaceComponent(new VelocityComponent { Value = authoritativeVelocity });

                // Predict where the entity should be right now based on the last server update.
                var clientTick = _tickSync.ClientTick;
                var tickDifference = clientTick > serverTick ? clientTick - serverTick : 0;
                var extrapolatedPosition = authoritativePosition + authoritativeVelocity * tickDifference * deltaTime;

                // Smoothly interpolate the current position towards the extrapolated position.
                // This avoids snapping and makes corrections appear smoother.
                positionComponent.Value = Vector3.Lerp(positionComponent.Value, extrapolatedPosition, 0.2f);
                return;
            }

            if (!_prediction.GetPredictedState(serverTick, out var predictedState))
            {
                // This can happen on startup or if the client is lagging severely.
                // We don't have a prediction to compare against, so we can't reconcile.
                return;
            }

            // 3. Compare and check for error.
            float error = Vector3.Distance(predictedState.Position, authoritativePosition);
            if (error > ReconciliationThreshold)
            {
                _logger.Debug($"Reconciliation needed at tick {serverTick}. Error: {error}");

                // 4. Instruct the prediction system to correct its state and re-simulate.
                _prediction.CorrectStateAndResimulate(serverTick, authoritativePosition, authoritativeVelocity);
                
                // 5. Get the re-simulated position for the *current* client tick and update the entity.
                if (_prediction.GetPredictedState(_tickSync.ClientTick, out var newlyPredictedState))
                {
                    entity.AddOrReplaceComponent(new PositionComponent { Value = newlyPredictedState.Position });
                    entity.AddOrReplaceComponent(new VelocityComponent { Value = newlyPredictedState.Velocity });
                }
            }
            
            // 6. Prune the state buffer to prevent memory leaks.
            _prediction.PruneOldStates(serverTick);
        }
    }
}