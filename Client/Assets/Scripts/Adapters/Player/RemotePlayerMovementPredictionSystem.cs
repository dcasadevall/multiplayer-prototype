using System.Linq;
using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Prediction;
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
                // Get the current client-side position and update velocity to the latest from the server.
                var positionComponent = entity.GetRequired<PositionComponent>();
                var authoritativePositionComponent = entity.GetRequired<PredictedComponent<PositionComponent>>();
                var authoritativeVelocityComponent = entity.GetRequired<PredictedComponent<VelocityComponent>>();
                if (authoritativePositionComponent.ServerValue == null || authoritativeVelocityComponent.ServerValue == null)
                {
                    _logger.Warn("Remote player entity {0} does not have authoritative position or velocity.", entity.Id);
                    continue;
                }
                
                // Set the authoritative position and velocity to the entity.
                var authoritativePosition = authoritativePositionComponent.ServerValue.Value;
                entity.AddOrReplaceComponent(new VelocityComponent { Value = authoritativePosition });

                // Predict where the entity should be right now based on the last server update.
                var clientTick = _tickSync.ClientTick;
                var serverTick = _tickSync.ServerTick;
                var tickDifference = clientTick > serverTick ? clientTick - serverTick : 0;
                var authoritativeVelocity = authoritativeVelocityComponent.ServerValue.Value;
                var extrapolatedPosition = authoritativePosition + authoritativeVelocity * tickDifference * deltaTime;

                // Smoothly interpolate the current position towards the extrapolated position.
                // This avoids snapping and makes corrections appear smoother.
                positionComponent.Value = Vector3.Lerp(positionComponent.Value, extrapolatedPosition, 0.2f);
            }
        }
    }
}