using System.Numerics;
using Shared;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Physics;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Listens for <see cref="PlayerMovementMessage"/> messages from the network and updates the velocity of the corresponding player entity.
    /// </summary>
    public class PlayerMovementHandler(EntityRegistry entityRegistry, IMessageReceiver messageReceiver, ILogger logger)
        : IInitializable, IDisposable
    {
        private IDisposable? _subscription;

        public void Initialize()
        {
            _subscription = messageReceiver.RegisterMessageHandler<PlayerMovementMessage>(GetType().Name, HandlePlayerMovementMessage);
        }

        /// <summary>
        /// Handles incoming <see cref="PlayerMovementMessage"/> messages and applies movement to the corresponding player entity.
        /// </summary>
        /// <param name="peerId">The network peer ID of the sender.</param>
        /// <param name="msg">The movement message containing input data.</param>
        private void HandlePlayerMovementMessage(int peerId, PlayerMovementMessage msg)
        {
            // Get the local player entity by peer ID
            var entity = entityRegistry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == peerId);

            if (entity == null)
            {
                logger.Warn(LoggedFeature.Input, $"Received PlayerMovementMessage from peer {peerId} but no player entity found.");
                return;
            }

            var moveDirection = new Vector3(msg.MoveDirection.X, 0, msg.MoveDirection.Y);
            var velocity = moveDirection * GameplayConstants.PlayerSpeed;

            entity.AddOrReplaceComponent(new VelocityComponent { Value = velocity });

            // Face the direction of movement
            if (moveDirection.LengthSquared() > 0)
            {
                var rotation = Quaternion.CreateFromYawPitchRoll(
                    MathF.Atan2(moveDirection.X, moveDirection.Z),
                    0,
                    0
                );
                entity.AddOrReplaceComponent(new RotationComponent { Value = rotation });
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}