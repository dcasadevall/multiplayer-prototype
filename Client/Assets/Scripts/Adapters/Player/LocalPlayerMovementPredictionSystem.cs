using System.Collections.Generic;
using System.Linq;
using Core.Input;
using Shared;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;
using Vector3 = System.Numerics.Vector3;

namespace Adapters.Player
{
    public struct PredictedState
    {
        public uint Tick;
        public Vector3 Position;
        public Vector3 Velocity;
    }

    public class LocalPlayerMovementPredictionSystem : ISystem
    {
        private readonly IInputListener _inputListener;
        private readonly TickSync _tickSync;
        private readonly ILogger _logger;
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();
        private readonly int _localPeerId;
        private float _lastDeltaTime;

        public LocalPlayerMovementPredictionSystem(IInputListener inputListener, IClientConnection connection, TickSync tickSync, ILogger logger)
        {
            _inputListener = inputListener;
            _tickSync = tickSync;
            _localPeerId = connection.AssignedPeerId;
            _logger = logger;
        }

        public bool GetPredictedState(uint tick, out PredictedState state) => _stateBuffer.TryGetValue(tick, out state);

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            _lastDeltaTime = deltaTime;
            var currentTick = _tickSync.ClientTick;
            var localPlayerEntity = registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .Where(x => x.Has<PredictedComponent<PositionComponent>>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == _localPeerId);
            
            if (localPlayerEntity == null) return;
            
            // Get the position from the previous tick to predict the next one.
            // If the buffer is empty, use the entity's current position.
            Vector3 lastPosition;
            if (_stateBuffer.TryGetValue(currentTick - 1, out var lastState))
            {
                lastPosition = lastState.Position;
            }
            else if(localPlayerEntity.TryGet<PositionComponent>(out var pos))
            {
                lastPosition = pos.Value;
            }
            else
            {
                // Cannot predict without a starting position.
                return;
            }

            // Default to zero velocity. Only apply a non-zero velocity if there is input for the current tick.
            var newVelocity = Vector3.Zero;
            if (_inputListener.TryGetMovementAtTick(currentTick, out var input))
            {
                var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                newVelocity = moveDirection * InputConstants.PlayerSpeed;
            }

            // Store the new predicted state and update the entity.
            var newPredictedPos = lastPosition + newVelocity * deltaTime;
            
            _stateBuffer[currentTick] = new PredictedState { Tick = currentTick, Position = newPredictedPos, Velocity = newVelocity };
            
            localPlayerEntity.AddOrReplaceComponent(new PositionComponent { Value = newPredictedPos });
            localPlayerEntity.AddOrReplaceComponent(new VelocityComponent { Value = newVelocity });
        }

        public void CorrectStateAndResimulate(uint authoritativeTick, Vector3 authoritativePosition, Vector3 authoritativeVelocity)
        {
            // 1. Correct the history with the server's authoritative state.
            _stateBuffer[authoritativeTick] = new PredictedState { Tick = authoritativeTick, Position = authoritativePosition, Velocity = authoritativeVelocity };
            var deltaTime = _lastDeltaTime == 0 ? (float)(1.0 / SharedConstants.WorldTickRate) : _lastDeltaTime;

            // 2. Re-simulate and update the buffer from that point forward to the present.
            for (uint tick = authoritativeTick + 1; tick <= _tickSync.ClientTick; tick++)
            {
                // Get the corrected state from the previous tick
                var previousState = _stateBuffer[tick - 1];
                var newVelocity = Vector3.Zero;

                if (_inputListener.TryGetMovementAtTick(tick, out var input))
                {
                    var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                    newVelocity = moveDirection * InputConstants.PlayerSpeed;
                }

                var newPredictedPos = previousState.Position + newVelocity * deltaTime;
                _stateBuffer[tick] = new PredictedState { Tick = tick, Position = newPredictedPos, Velocity = newVelocity };
            }
        }
        
        // Clean up old states to prevent memory leaks.
        public void PruneOldStates(uint lastServerTick)
        {
            // Remove all buffered states older than the last authoritative tick from the server.
            // We keep a small buffer just in case of out-of-order packets.
            var cutoffTick = lastServerTick > 20 ? lastServerTick - 20 : 0;
            var oldKeys = _stateBuffer.Keys.Where(k => k < cutoffTick).ToList();
            foreach (var key in oldKeys)
            {
                _stateBuffer.Remove(key);
            }
        }
    }
}