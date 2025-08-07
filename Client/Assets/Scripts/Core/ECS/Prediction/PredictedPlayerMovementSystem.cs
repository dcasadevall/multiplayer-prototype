using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.ECS.Entities;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using Shared.Networking.Messages;
using ILogger = Shared.Logging.ILogger;
using Vector3 = System.Numerics.Vector3;

namespace Core.ECS.Prediction
{
    /// <summary>
    /// Handles local player movement including input capture, prediction, and reconciliation.
    /// Only operates on the local player entity with PlayerTagComponent.
    /// Sends the intended movement input to the server for authoritative processing.
    /// </summary>
    public class PredictedPlayerMovementSystem : ISystem
    {
        private readonly IMessageSender _messageSender;
        private readonly IInputListener _inputListener;
        private readonly ITickSync _tickSync;
        private readonly ILogger _logger;
        private readonly int _localPeerId;
        
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();

        // How far off the predicted position can be before we need to reconcile with the server
        private const float ReconciliationThreshold = 0.1f;
        // How quickly to correct the error. Lower is smoother
        private const float ReconciliationSmoothingFactor = 0.15f;

        // Stores the positional error that needs to be smoothed out.
        private Vector3 _reconciliationError = Vector3.Zero;
        
        // Store last input sent to the server to avoid sending duplicate inputs
        private Vector2 _lastMovementSent = Vector2.Zero;

        private struct PredictedState
        {
            public Vector3 Position;
        }

        public PredictedPlayerMovementSystem(
            IClientConnection clientConnection,
            IMessageSender messageSender,
            IInputListener inputListener,
            ITickSync tickSync,
            ILogger logger)
        {
            _messageSender = messageSender;
            _inputListener = inputListener;
            _tickSync = tickSync;
            _logger = logger;
            _localPeerId = clientConnection.AssignedPeerId;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var localPlayer = registry.GetLocalPlayerEntity(_localPeerId);

            if (localPlayer == null) return;
            
            // Send any new movement input to the server
            // This could be done in a separate system,
            // but we handle it here to keep all the local movement logic together.
            SendMovementInputIfNecessary(tickNumber);

            // Apply prediction and smoothing
            ProcessLocalPlayerMovement(localPlayer, tickNumber, deltaTime);

            // Check for reconciliation against server state
            CheckReconciliation(localPlayer, tickNumber, deltaTime);

            // Clean up old states from the buffer
            PruneOldStates(_tickSync.ServerTick);
        }

        private void SendMovementInputIfNecessary(uint clientTick)
        {
            // If the input listener has no movement at this tick, we don't need to send anything.
            if (!_inputListener.TryGetMovementAtTick(clientTick, out var moveDirection))
            {
                return;
            }
            
            // Only send an update to the server if the input state has actually changed.
            if (moveDirection == _lastMovementSent) return;
            
            var playerMovementMsg = new PlayerMovementMessage
            {
                ClientTick = clientTick,
                MoveDirection = moveDirection
            };
            
            // Send the new input state to the server.
            _messageSender.SendMessageToServer(MessageType.PlayerMovement, playerMovementMsg);

            // Update the last sent move direction.
            _lastMovementSent = moveDirection;
        }

        private void ProcessLocalPlayerMovement(Entity localPlayer, uint currentTick, float deltaTime)
        {
            var position = localPlayer.GetRequired<PositionComponent>();

            var lastState = _stateBuffer.TryGetValue(currentTick - 1, out var state)
                ? state
                : new PredictedState { Position = position.Value };

            var newVelocity = Vector3.Zero;
            if (_inputListener.TryGetMovementAtTick(currentTick, out var moveDirection))
            {
                newVelocity = new Vector3(moveDirection.X, 0, moveDirection.Y) * GameplayConstants.PlayerSpeed;
            }

            // 1. Calculate the pure, uncorrected prediction for this tick.
            var newPosition = lastState.Position + newVelocity * deltaTime;

            // Store this pure state in our history.
            _stateBuffer[currentTick] = new PredictedState { Position = newPosition };

            // 2. Smoothly reduce any existing reconciliation error each frame.
            // This is the "suspension" doing its work.
            _reconciliationError = Vector3.Lerp(_reconciliationError, Vector3.Zero, ReconciliationSmoothingFactor);

            // 3. The final visual position is our pure prediction plus the diminishing error.
            localPlayer.AddOrReplaceComponent(new PositionComponent { Value = newPosition + _reconciliationError });
            localPlayer.AddOrReplaceComponent(new VelocityComponent { Value = newVelocity });
        }

        private void CheckReconciliation(Entity localPlayer, uint currentTick, float deltaTime)
        {
            var predictedComponent = localPlayer.GetRequired<PredictedComponent<PositionComponent>>();
            if (!predictedComponent.HasServerValue) return;

            // The server tick that this position data represents
            uint serverDataTick = _tickSync.ServerTick;
    
            // We need to have a predicted state for this tick to compare against
            if (!_stateBuffer.TryGetValue(serverDataTick, out var predictedStateOnThatTick)) return;

            var serverPosition = predictedComponent.ServerValue!.Value;
            var error = Vector3.Distance(predictedStateOnThatTick.Position, serverPosition);

            if (error > ReconciliationThreshold)
            {
                _logger.Debug($"Reconciliation needed at tick {serverDataTick}. Error: {error:F3}");

                // Store current visual position before correction
                var currentVisualPosition = localPlayer.GetRequired<PositionComponent>().Value;

                // Correct and re-simulate using the same deltaTime as original predictions
                CorrectStateAndResimulate(serverDataTick, serverPosition, deltaTime);

                // Calculate new error after re-simulation
                if (_stateBuffer.TryGetValue(currentTick, out var correctedCurrentState))
                {
                    _reconciliationError = currentVisualPosition - correctedCurrentState.Position;
                }
            }
        }

        private void CorrectStateAndResimulate(uint authoritativeTick, Vector3 authoritativePosition, float deltaTime)
        {
            // Correct the state at the authoritative tick with server data
            _stateBuffer[authoritativeTick] = new PredictedState
            {
                Position = authoritativePosition,
            };

            // Re-simulate from the corrected tick forward to the current client tick
            for (uint tick = authoritativeTick + 1; tick <= _tickSync.ClientTick; tick++)
            {
                var previousState = _stateBuffer[tick - 1];
                var newVelocity = Vector3.Zero;

                // Apply the historical input for this tick
                if (_inputListener.TryGetMovementAtTick(tick, out var moveDirection))
                {
                    newVelocity = new Vector3(moveDirection.X, 0, moveDirection.Y) * GameplayConstants.PlayerSpeed;
                }

                var newPosition = previousState.Position + newVelocity * deltaTime;
                _stateBuffer[tick] = new PredictedState
                {
                    Position = newPosition,
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