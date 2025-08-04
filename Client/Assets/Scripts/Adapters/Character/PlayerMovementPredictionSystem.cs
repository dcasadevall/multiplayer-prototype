using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Core.Input;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Networking;

namespace Adapters.Character
{
    public struct PredictedState
    {
        public uint Tick;
        public Vector3 Position;
    }

    public class PlayerMovementPredictionSystem : ISystem
    {
        private readonly IInputListener _inputListener;
        private readonly TickSync _tickSync;
        private readonly Dictionary<uint, PredictedState> _stateBuffer = new();
        private readonly int _localPeerId;

        public bool GetPredictedState(uint tick, out PredictedState state) => _stateBuffer.TryGetValue(tick, out state);
        
        public PlayerMovementPredictionSystem(IInputListener inputListener, IClientConnection connection, TickSync tickSync)
        {
            _inputListener = inputListener;
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

            var tick = _tickSync.ClientTick;
            var clientPosition = localPlayerEntity.GetRequired<PositionComponent>().Value;
            if (_inputListener.TryGetMovementAtTick(tick, out var input))
            {
                var moveDelta = new Vector3(input.MoveDirection.X, 0, input.MoveDirection.Y) * 5f * deltaTime;
                var newPos = clientPosition + moveDelta;

                _stateBuffer[tick] = new PredictedState { Tick = tick, Position = newPos };
                localPlayerEntity.AddOrReplaceComponent(new PositionComponent { Value = newPos });
            }
        }
    }
}
