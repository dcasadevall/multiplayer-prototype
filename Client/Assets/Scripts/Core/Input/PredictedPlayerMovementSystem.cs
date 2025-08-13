using System;
using System.Collections.Generic;
using System.Linq;
using Core.ECS.Entities;
using Shared;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Physics;
using Shared.Prediction;
using Shared.Settings;
using ILogger = Shared.Logging.ILogger;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Quaternion = System.Numerics.Quaternion;

namespace Core.Input
{
    /// <summary>
    /// Handles local player movement including input capture, prediction, smoothing and reconciliation.
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
        private readonly PlayerSettings _playerSettings;
        private readonly SimulationSettings _simulationSettings;
        
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();

        // Threshold to trigger reconciliation against server state (meters)
        private const float ReconciliationThreshold = 0.10f;

        // Tighter epsilon to early-out tiny corrections (meters)
        private const float ReconcileEpsilon = 0.02f;

        // Exponential smoothing constant (higher = faster decay of visual error)
        private const float ReconciliationLambda = 10f;

        // Visual-only reconciliation error that we decay over time
        private Vector3 _reconciliationError = Vector3.Zero;

        // Avoid sending redundant input states
        private Vector2 _lastMovementSent = Vector2.Zero;

        // Track the furthest tick we've actually predicted to; used to bound resimulation
        private uint _lastPredictedTick;

        private struct PredictedState
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public PredictedPlayerMovementSystem(
            IClientConnection clientConnection,
            IMessageSender messageSender,
            IInputListener inputListener,
            ITickSync tickSync,
            ILogger logger,
            PlayerSettings playerSettings,
            SimulationSettings simulationSettings)
        {
            _messageSender = messageSender;
            _inputListener = inputListener;
            _tickSync = tickSync;
            _logger = logger;
            _playerSettings = playerSettings;
            _simulationSettings = simulationSettings;
            _localPeerId = clientConnection.AssignedPeerId;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var localPlayer = registry.GetLocalPlayerEntity(_localPeerId);
            if (localPlayer == null) return;

            // 1) Send input delta to server
            SendMovementInputIfNecessary(tickNumber);

            // 2) Predict locally using Δt to mirror server integration exactly
            ProcessLocalPlayerMovement(localPlayer, tickNumber);

            // 3) Reconcile against server authoritative state (when available)
            CheckReconciliation(localPlayer, tickNumber);

            // 4) Keep only a sliding window around last known server tick
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

            var msg = new PlayerMovementMessage
            {
                ClientTick = clientTick,
                MoveDirection = moveDirection
            };

            _messageSender.SendMessageToServer(MessageType.PlayerMovement, msg);
            _lastMovementSent = moveDirection;
        }

        private void ProcessLocalPlayerMovement(Entity localPlayer, uint currentTick)
        {
            var position = localPlayer.GetRequired<PositionComponent>();
            var rotation = localPlayer.GetRequired<RotationComponent>();

            // Get last predicted state; if none, seed from current components
            var lastState = _stateBuffer.TryGetValue(currentTick - 1, out var state)
                ? state
                : new PredictedState { Position = position.Value, Rotation = rotation.Value };

            Vector3 velocity = Vector3.Zero;
            var newRotation = lastState.Rotation;

            if (_inputListener.TryGetMovementAtTick(currentTick, out var rawDir) &&
                rawDir.LengthSquared() > 0.01f)
            {
                var dir = Vector2.Normalize(rawDir);
                
                // Convention: X = right, Z = forward (Unity world). Map (x,y) -> (x,0,z=y).
                velocity = new Vector3(dir.X, 0f, dir.Y) * _playerSettings.PlayerSpeed;
                newRotation = Quaternion.CreateFromYawPitchRoll(MathF.Atan2(dir.X, dir.Y), 0f, 0f);
            }

            // Use fixed delta to match server exactly
            float dt = (float)_simulationSettings.FixedDeltaTime.TotalSeconds;
            var newPosition = lastState.Position + velocity * dt;

            // Store this pure state in our history.
            _stateBuffer[currentTick] = new PredictedState { Position = newPosition, Rotation = newRotation };
            _lastPredictedTick = Math.Max(currentTick, _lastPredictedTick);

            // Exponential smoothing of visual reconciliation error; rate independent of tick rate
            float alpha = 1f - MathF.Exp(-ReconciliationLambda * dt);
            _reconciliationError = Vector3.Lerp(_reconciliationError, Vector3.Zero, alpha);

            // Write visual components (predicted + decaying error)
            localPlayer.AddOrReplaceComponent(new PositionComponent { Value = newPosition + _reconciliationError });
            localPlayer.AddOrReplaceComponent(new VelocityComponent { Value = velocity });
            localPlayer.AddOrReplaceComponent(new RotationComponent { Value = newRotation });
        }

        private void CheckReconciliation(Entity localPlayer, uint currentTick)
        {
            var predictedComponent = localPlayer.GetRequired<PredictedComponent<PositionComponent>>();
            if (!predictedComponent.HasServerValue) return;

            // The server tick is the authoritative tick for this reconciliation check
            // Since the replication tick rate is == to the simulation tick rate for this sample.
            uint serverDataTick = _tickSync.ServerTick;

            if (!_stateBuffer.TryGetValue(serverDataTick, out var predictedAtServerTick))
            {
                // We don't have history for that tick; nothing to compare against.
                predictedComponent.ServerValue = null;
                return;
            }

            var serverPos = predictedComponent.ServerValue!.Value;
            var errorSq = Vector3.DistanceSquared(predictedAtServerTick.Position, serverPos);

            if (errorSq >= ReconciliationThreshold * ReconciliationThreshold)
            {
                _logger.Debug(LoggedFeature.Prediction, $"Reconciliation at tick {serverDataTick}. Error={MathF.Sqrt(errorSq):F3}m");
                var currentVisual = localPlayer.GetRequired<PositionComponent>().Value;
                CorrectStateAndResimulate(serverDataTick, serverPos);

                if (_stateBuffer.TryGetValue(currentTick, out var correctedNow))
                {
                    // Visual error is the delta from what we were showing to the corrected prediction
                    _reconciliationError = currentVisual - correctedNow.Position;
                }
            }

            // Consume server value
            predictedComponent.ServerValue = null;
        }

        private void CorrectStateAndResimulate(uint authoritativeTick, Vector3 authoritativePosition)
        {
            // Early-out on tiny corrections
            if (_stateBuffer.TryGetValue(authoritativeTick, out var before) &&
                Vector3.DistanceSquared(before.Position, authoritativePosition) < ReconcileEpsilon * ReconcileEpsilon)
            {
                return;
            }

            // Keep client-rotation at that tick (server isn't correcting rotation)
            var keepRotation = _stateBuffer.TryGetValue(authoritativeTick, out var oldState)
                ? oldState.Rotation
                : Quaternion.Identity;

            _stateBuffer[authoritativeTick] = new PredictedState
            {
                Position = authoritativePosition,
                Rotation = keepRotation
            };

            // Only resim ticks we had actually predicted (avoid fabricating future ticks)
            uint resimEnd = _lastPredictedTick;
            if (authoritativeTick >= resimEnd)
            {
                return;
            }

            float dt = (float)_simulationSettings.FixedDeltaTime.TotalSeconds;

            for (uint tick = authoritativeTick + 1; tick <= resimEnd; tick++)
            {
                if (!_stateBuffer.TryGetValue(tick - 1, out var prev))
                {
                    // Missing history (pruned or arrived late) — stop here safely
                    break;
                }

                Vector3 velocity = Vector3.Zero;
                var rot = prev.Rotation;

                if (_inputListener.TryGetMovementAtTick(tick, out var rawDir) &&
                    rawDir.LengthSquared() > 0.01f)
                {
                    var dir = Vector2.Normalize(rawDir);
                    velocity = new Vector3(dir.X, 0f, dir.Y) * _playerSettings.PlayerSpeed;
                    rot = Quaternion.CreateFromYawPitchRoll(MathF.Atan2(dir.X, dir.Y), 0f, 0f);
                }

                var pos = prev.Position + velocity * dt;
                _stateBuffer[tick] = new PredictedState { Position = pos, Rotation = rot };
            }
        }

        private void PruneOldStates(uint lastServerTick)
        {
            // Keep a small lookback window behind the latest server tick (e.g., 20 ticks)
            uint keepFrom = lastServerTick > 20 ? lastServerTick - 20 : 0;
            var toRemove = _stateBuffer.Keys.Where(k => k < keepFrom).ToList();
            toRemove.ForEach(k => _stateBuffer.Remove(k));
        }
    }
}
