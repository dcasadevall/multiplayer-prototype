using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Logging;
using Shared.Networking;

namespace Adapters.Character
{
    public struct PredictedState
    {
        public uint Tick;
        public Vector3 Position;
        // You might also want to store velocity, rotation, etc.
    }

    public class PlayerMovementPredictionSystem : ISystem
    {
        private readonly IInputListener _inputListener;
        private readonly TickSync _tickSync;
        private readonly ILogger _logger;
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();
        private readonly int _localPeerId;

        // NOTE: Centralized movement logic.
        // Let's assume a fixed tick rate, so deltaTime is constant. 
        // Using a fixed speed value is more deterministic for lockstep/tick-based simulation.
        private const float Speed = 5.0f; 
        private const float TickRate = 60.0f; // Example tick rate
        private const float MoveDeltaPerTick = Speed / TickRate;

        public PlayerMovementPredictionSystem(IInputListener inputListener, IClientConnection connection, TickSync tickSync, ILogger logger)
        {
            _inputListener = inputListener;
            _tickSync = tickSync;
            _localPeerId = connection.AssignedPeerId;
            _logger = logger;
        }

        public bool GetPredictedState(uint tick, out PredictedState state) => _stateBuffer.TryGetValue(tick, out state);

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // The tickNumber here should be the client's current simulation tick.
            // Ensure this is consistent with _tickSync.ClientTick.
            var currentTick = _tickSync.ClientTick;

            var localPlayerEntity = GetLocalPlayerEntity(registry);
            if (localPlayerEntity == null)
            {
                return;
            }

            // Get the position from the previous tick to predict the next one.
            // If the buffer is empty, use the entity's current position.
            Vector3 lastPosition;
            if (_stateBuffer.TryGetValue(currentTick - 1, out var lastState))
            {
                lastPosition = lastState.Position;
            }
            else
            {
                lastPosition = localPlayerEntity.GetRequired<PositionComponent>().Value;
            }
            
            var newPredictedPos = lastPosition;
            if (_inputListener.TryGetMovementAtTick(currentTick, out var input))
            {
                // NOTE: Use the consistent movement calculation.
                var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                newPredictedPos += moveDirection * MoveDeltaPerTick;
            }
            else
            {
                _logger.Warn($"No input found for tick {currentTick}. Using last position {lastPosition}.");
            }

            // Store the new predicted state and update the entity.
            _stateBuffer[currentTick] = new PredictedState { Tick = currentTick, Position = newPredictedPos };
            localPlayerEntity.AddOrReplaceComponent(new PositionComponent { Value = newPredictedPos });
        }
        
        // NEW: This is the core of the fix.
        // It's called by the Reconciliation system when an error is detected.
        public void CorrectStateAndResimulate(uint authoritativeTick, Vector3 authoritativePosition)
        {
            // 1. Correct the history with the server's authoritative state.
            _stateBuffer[authoritativeTick] = new PredictedState { Tick = authoritativeTick, Position = authoritativePosition };

            // 2. Re-simulate and update the buffer from that point forward to the present.
            for (uint tick = authoritativeTick + 1; tick <= _tickSync.ClientTick; tick++)
            {
                // Get the corrected state from the previous tick
                var previousState = _stateBuffer[tick - 1];
                var newPredictedPos = previousState.Position;

                if (_inputListener.TryGetMovementAtTick(tick, out var input))
                {
                    var moveDirection = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y);
                    newPredictedPos += moveDirection * MoveDeltaPerTick;
                }

                _stateBuffer[tick] = new PredictedState { Tick = tick, Position = newPredictedPos };
            }
        }
        
        // NEW: Clean up old states to prevent memory leaks.
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

        private Entity GetLocalPlayerEntity(EntityRegistry registry)
        {
            // This is just to avoid code duplication.
            return registry
                .GetAll()
                .Where(x => x.Has<PeerComponent>() && x.Has<PlayerTagComponent>() && x.Has<PredictedComponent<PositionComponent>>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == _localPeerId);
        }
    }
}