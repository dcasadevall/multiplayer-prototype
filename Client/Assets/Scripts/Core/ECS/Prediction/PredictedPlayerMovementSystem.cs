using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using ILogger = Shared.Logging.ILogger;
using Vector3 = System.Numerics.Vector3;

namespace Core.ECS.Prediction
{
    /// <summary>
    /// Handles local player movement including input capture, prediction, and reconciliation.
    /// Only operates on the local player entity with PlayerTagComponent.
    /// </summary>
    public class PredictedPlayerMovementSystem : ISystem
    {
        private readonly IInputListener _inputListener;
        private readonly TickSync _tickSync;
        private readonly ILogger _logger;
        private readonly IClientConnection _clientConnection;
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();

        // Reconciliation settings
        private const float ReconciliationThreshold = 0.1f; // Reduced threshold for more responsive corrections
        private const float ReconciliationSmoothingFactor = 0.15f; // How quickly to correct the error. Lower is smoother.

        // Stores the positional error that needs to be smoothed out.
        private Vector3 _reconciliationError = Vector3.Zero;

        public struct PredictedState
        {
            public Vector3 Position;
            public Vector3 Velocity;
        }

        public PredictedPlayerMovementSystem(
            IInputListener inputListener,
            IClientConnection clientConnection,
            TickSync tickSync,
            ILogger logger)
        {
            _inputListener = inputListener;
            _tickSync = tickSync;
            _clientConnection = clientConnection;
            _logger = logger;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var clientTick = _tickSync.ClientTick;
            var localPlayer = GetLocalPlayerEntity(registry);

            if (localPlayer == null) return;

            // Apply prediction and smoothing
            ProcessLocalPlayerMovement(localPlayer, clientTick, deltaTime);

            // Check for reconciliation against server state
            CheckReconciliation(localPlayer, deltaTime);

            // Clean up old states from the buffer
            PruneOldStates(_tickSync.ServerTick);
        }

        private Entity GetLocalPlayerEntity(EntityRegistry registry)
        {
            return registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PredictedComponent<PositionComponent>>())
                .Where(x => x.Has<PredictedComponent<VelocityComponent>>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == _clientConnection.AssignedPeerId);
        }

        private void ProcessLocalPlayerMovement(Entity localPlayer, uint clientTick, float deltaTime)
        {
            var position = localPlayer.GetRequired<PositionComponent>();
            var velocity = localPlayer.GetRequired<VelocityComponent>();

            // Get the last known position to predict from.
            // If no history, use the current position.
            Vector3 lastPosition;
            if (_stateBuffer.TryGetValue(clientTick - 1, out var lastState))
            {
                lastPosition = lastState.Position;
            }
            else
            {
                lastPosition = position.Value;
            }

            // Calculate velocity based on current input
            var newVelocity = Vector3.Zero;
            if (_inputListener.TryGetMovementAtTick(clientTick, out var input))
            {
                var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                newVelocity = moveDirection * InputConstants.PlayerSpeed;
            }

            // Predict the new position for this tick
            var newPosition = lastPosition + newVelocity * deltaTime;

            // Store the purely predicted state in the buffer
            _stateBuffer[clientTick] = new PredictedState
            {
                Position = newPosition,
                Velocity = newVelocity
            };

            // Smoothly reduce the reconciliation error over time
            _reconciliationError = Vector3.Lerp(_reconciliationError, Vector3.Zero, ReconciliationSmoothingFactor);

            // Apply the smoothed error to the predicted position for the final visual position
            position.Value = newPosition + _reconciliationError;
            velocity.Value = newVelocity;
        }

        private void CheckReconciliation(Entity localPlayer, float deltaTime)
        {
            var predictedPosition = localPlayer.GetRequired<PredictedComponent<PositionComponent>>();
            var serverTick = _tickSync.ServerTick;

            // We only reconcile if we have a new server state for a tick we have already predicted.
            if (!predictedPosition.HasServerValue || serverTick == 0 || !_stateBuffer.TryGetValue(serverTick, out var predictedStateOnServerTick))
                return;

            var serverPosition = predictedPosition.ServerValue.Value;
            var serverVelocity = localPlayer.GetRequired<PredictedComponent<VelocityComponent>>().ServerValue.Value;

            // Calculate the prediction error
            var error = Vector3.Distance(predictedStateOnServerTick.Position, serverPosition);

            if (error > ReconciliationThreshold)
            {
                _logger.Debug($"Reconciliation needed at tick {serverTick}. Error: {error:F3}");

                // Get the current visual position before correcting history
                var currentVisualPosition = localPlayer.GetRequired<PositionComponent>().Value;

                // Correct the historical state and re-simulate future inputs
                CorrectStateAndResimulate(serverTick, serverPosition, serverVelocity, deltaTime);

                // Get the newly re-simulated position for the current client tick
                if (_stateBuffer.TryGetValue(_tickSync.ClientTick, out var reSimulatedCurrentState))
                {
                    // Instead of snapping, calculate the error between where we are now
                    // and where we should be. This error will be smoothed out over time.
                    _reconciliationError = currentVisualPosition - reSimulatedCurrentState.Position;
                }
            }
            
            // Mark the server value as processed by clearing it
            predictedPosition.ServerValue = null;
        }

        private void CorrectStateAndResimulate(uint authoritativeTick, Vector3 authoritativePosition, Vector3 authoritativeVelocity, float deltaTime)
        {
            // Correct the state at the authoritative tick with server data
            _stateBuffer[authoritativeTick] = new PredictedState
            {
                Position = authoritativePosition,
                Velocity = authoritativeVelocity
            };

            // Re-simulate from the corrected tick forward to the current client tick
            for (uint tick = authoritativeTick + 1; tick <= _tickSync.ClientTick; tick++)
            {
                var previousState = _stateBuffer[tick - 1];
                var newVelocity = Vector3.Zero;

                // Apply the historical input for this tick
                if (_inputListener.TryGetMovementAtTick(tick, out var input))
                {
                    var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                    newVelocity = moveDirection * InputConstants.PlayerSpeed;
                }

                var newPosition = previousState.Position + newVelocity * deltaTime;
                _stateBuffer[tick] = new PredictedState
                {
                    Position = newPosition,
                    Velocity = newVelocity
                };
            }
        }

        private void PruneOldStates(uint lastServerTick)
        {
            // Keep a small buffer of states before the last known server tick
            var cutoffTick = lastServerTick > 20 ? lastServerTick - 20 : 0;
            var oldKeys = _stateBuffer.Keys.Where(k => k < cutoffTick).ToList();

            foreach (var key in oldKeys)
            {
                _stateBuffer.Remove(key);
            }
        }
    }
}