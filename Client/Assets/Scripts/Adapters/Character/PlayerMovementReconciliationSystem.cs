using System.Linq;
using System.Numerics;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking;

namespace Adapters.Character
{
    public class PlayerMovementReconciliationSystem : ISystem
    {
        private PlayerMovementPredictionSystem _prediction;
        private IInputListener _input;
        private readonly ILogger _logger;
        private readonly TickSync _tickSync;
        private readonly int _localPeerId;

        public PlayerMovementReconciliationSystem(PlayerMovementPredictionSystem prediction, 
            IClientConnection connection,
            IInputListener input,
            ILogger logger,
            TickSync tickSync)
        {
            _prediction = prediction;
            _input = input;
            _logger = logger;
            _tickSync = tickSync;
            _localPeerId = connection.AssignedPeerId;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var localPlayerEntity = registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PredictedComponent<PositionComponent>>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == _localPeerId);

            if (localPlayerEntity == null)
            {
                return;
            }
            
            // Get predicted state at server tick
            if (!_prediction.GetPredictedState(_tickSync.ServerTick, out var predictedState))
            {
                // _logger.Warn($"Tick {_tickSync.ClientTick}: Predicted state at server tick {_tickSync.ServerTick} not found.");
                return;
            }
            
            var authoritativePosition = localPlayerEntity.GetRequired<PredictedComponent<PositionComponent>>();
            if (authoritativePosition.ServerValue == null)
            {
                _logger.Warn($"Tick {_tickSync.ClientTick}: ServerValue for authoritative position is null.");
                return;
            }
            
            float error = Vector3.Distance(predictedState.Position, authoritativePosition.ServerValue.Value);
            if (error > 0.01f)
            {
                _logger.Debug($"Reconciliation at tick {_tickSync.ServerTick}. Error: {error}");

                localPlayerEntity.AddOrReplaceComponent(new PositionComponent
                {
                    Value = authoritativePosition.ServerValue.Value
                });

                // Re-simulate from serverTick to ClientTick
                var predictedPosition = authoritativePosition.ServerValue.Value;
                for (uint tick = _tickSync.ServerTick + 1; tick <= _tickSync.ClientTick; tick++)
                {
                    if (!_input.TryGetMovementAtTick(tick, out var inputMessage))
                    {
                        _logger.Warn($"Tick {tickNumber}: Movement at tick {tick} failed.");
                        continue;
                    }
                    
                    predictedPosition += new Vector3(inputMessage.MoveDirection.X, 0, inputMessage.MoveDirection.Y) * 0.1f;
                }

                // Update the local player's position to the predicted position
                localPlayerEntity.AddOrReplaceComponent(new PositionComponent
                {
                    Value = predictedPosition
                });
            }
        }
    }
}