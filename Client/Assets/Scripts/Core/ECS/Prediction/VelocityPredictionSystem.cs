using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Networking;
using ILogger = Shared.Logging.ILogger;
using Vector3 = System.Numerics.Vector3;

namespace Core.ECS.Prediction
{
    /// <summary>
    /// Handles velocity-based prediction and interpolation for all entities except the local player.
    /// Applies server-authoritative movement with smooth interpolation for remote entities.
    /// </summary>
    public class VelocityPredictionSystem : ISystem
    {
        private readonly ITickSync _tickSync;
        private readonly ILogger _logger;
        private readonly IClientConnection _clientConnection;
        
        // Interpolation settings
        private const float InterpolationSpeed = 0.5f;
        private const float MaxSnapDistance = 2.0f;

        public VelocityPredictionSystem(IClientConnection clientConnection, ITickSync tickSync, ILogger logger)
        {
            _tickSync = tickSync;
            _logger = logger;
            _clientConnection = clientConnection;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var currentTick = _tickSync.ClientTick;
            var serverTick = _tickSync.ServerTick;
            
            // Find all entities with predicted velocity, excluding the local player
            var predictedEntities = registry
                .GetAll()
                .Where(x => x.Has<PredictedComponent<VelocityComponent>>())
                .Where(x => x.Has<PositionComponent>())
                .Where(x => x.Has<VelocityComponent>())
                .Where(x => !IsLocalPlayer(x))
                .ToList();

            foreach (var entity in predictedEntities)
            {
                ProcessPredictedEntity(entity, currentTick, serverTick, deltaTime);
            }
        }

        private bool IsLocalPlayer(Entity entity)
        {
            if (!entity.Has<PlayerTagComponent>() || !entity.Has<PeerComponent>())
                return false;
                
            var peerComponent = entity.GetRequired<PeerComponent>();
            return peerComponent.PeerId == _clientConnection.AssignedPeerId;
        }

        private void ProcessPredictedEntity(Entity entity, uint currentTick, uint serverTick, float deltaTime)
        {
            var serverAuthorityVelocity = entity.GetRequired<PredictedComponent<VelocityComponent>>();
            var position = entity.GetRequired<PositionComponent>();
            var velocity = entity.GetRequired<VelocityComponent>();
            
            // Check if we have server data to work with
            if (!serverAuthorityVelocity.HasServerValue)
            {
                // No server data yet, just apply current velocity
                position.Value += velocity.Value * deltaTime;
                return;
            }
            
            // Update velocity from server data
            velocity.Value = serverAuthorityVelocity.ServerValue.Value;
            
            // Handle position prediction/interpolation
            if (false && entity.TryGet<PredictedComponent<PositionComponent>>(out var predictedPosition))
            {
                ProcessWithPredictedPosition(entity, predictedPosition, serverAuthorityVelocity, position, velocity, serverTick, deltaTime);
            }
            else
            {
                // No predicted position, just apply velocity
                position.Value += velocity.Value * deltaTime;
            }
        }

        private void ProcessWithPredictedPosition(
            Entity entity,
            PredictedComponent<PositionComponent> predictedPosition,
            PredictedComponent<VelocityComponent> predictedVelocity,
            PositionComponent position,
            VelocityComponent velocity,
            uint serverTick,
            float deltaTime)
        {
            if (!predictedPosition.HasServerValue)
            {
                // No server position data, just apply velocity
                position.Value += velocity.Value * deltaTime;
                return;
            }
            
            var serverPosition = predictedPosition.ServerValue.Value;
            var serverVelocity = predictedVelocity.ServerValue.Value;
            
            // Calculate extrapolated position based on server data and time difference
            var tickDifference = _tickSync.SmoothedTick > serverTick ? (int)(_tickSync.SmoothedTick - serverTick) : 0;
            
            // Predict where the entity should be now based on server data
            var predictedCurrentPosition = serverPosition + serverVelocity * (tickDifference * deltaTime);
            
            // Check if we need to snap due to large distance
            var distance = Vector3.Distance(position.Value, predictedCurrentPosition);
            
            if (distance > MaxSnapDistance)
            {
                // Large error - snap immediately
                position.Value = predictedCurrentPosition;
                _logger.Debug("Snapped entity {0} due to large distance: {1:F2}", entity.Id, distance);
            }
            else
            {
                // Small error - smooth interpolation
                position.Value = Vector3.Lerp(position.Value, predictedCurrentPosition, InterpolationSpeed);
            }
        }
    }
}