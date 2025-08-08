using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.TickSync;
using Shared.Networking;
using Shared.Physics;

namespace Core.Physics
{
    /// <summary>
    /// Updates the authoritative gameplay state for remote entities.
    /// This system runs on the fixed tick rate and does NOT perform any visual smoothing.
    /// It uses server data to calculate the "true" position via extrapolation
    /// and performs dead reckoning when server data is stale.
    /// </summary>
    public class VelocityPredictionSystem : ISystem
    {
        private readonly ITickSync _tickSync;
        private readonly IClientConnection _clientConnection;

        public VelocityPredictionSystem(IClientConnection clientConnection, ITickSync tickSync)
        {
            _tickSync = tickSync;
            _clientConnection = clientConnection;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float fixedDeltaTime)
        {
            var remoteEntities = registry.GetAll()
                .Where(x => x.Has<PositionComponent>() && x.Has<VelocityComponent>())
                .Where(x => !IsLocalPlayer(x));

            foreach (var entity in remoteEntities)
            {
                ProcessRemoteEntity(entity, fixedDeltaTime);
            }
        }

        private bool IsLocalPlayer(Entity entity)
        {
            if (!entity.TryGet(out PeerComponent peer)) return false;
            return peer.PeerId == _clientConnection.AssignedPeerId;
        }

        private void ProcessRemoteEntity(Entity entity, float fixedDeltaTime)
        {
            var localPos = entity.GetRequired<PositionComponent>();
            var localVel = entity.GetRequired<VelocityComponent>();

            // Try to get the server's authoritative state.
            var hasServerPos = entity.TryGet(out PredictedComponent<PositionComponent> predPos) && predPos.HasServerValue;
            var hasServerVel = entity.TryGet(out PredictedComponent<VelocityComponent> predVel) && predVel.HasServerValue;

            // --- State Update and Extrapolation (When fresh server data arrives) ---
            if (hasServerPos && hasServerVel)
            {
                // 1. Get the authoritative state from the server.
                var serverPosition = predPos.ServerValue!.Value;
                var serverVelocity = predVel.ServerValue!.Value;
                
                // Server data tick is the tick when the server sent this position and velocity.
                uint serverDataTick = _tickSync.ServerTick;

                // 2. Extrapolate: Calculate where the entity should be "now" based on the
                // historical server data and the time that has passed.
                var tickDifference = _tickSync.SmoothedTick > serverDataTick ? (int)(_tickSync.SmoothedTick - serverDataTick) : 0;
                var authoritativePosition = serverPosition + serverVelocity * (tickDifference * fixedDeltaTime);

                // 3. Update Local State: Snap the logical position and velocity
                // directly to the calculated authoritative values.
                localPos.Value = authoritativePosition;
                localVel.Value = serverVelocity;

                // 4. Clear the server values so we don't process this old data again.
                predPos.ServerValue = null;
                predVel.ServerValue = null;
            }
            else
            {
                // No fresh data. Keep the entity moving based on its last known velocity
                // to prevent stuttering between packets.
                // This is called dead reckoning.
                localPos.Value += localVel.Value * fixedDeltaTime;
            }
        }
    }
}