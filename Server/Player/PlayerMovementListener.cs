using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Input;
using Shared.Logging;
using Shared.Networking;
using Shared.Scheduling;

namespace Server.Player
{
    /// <summary>
    /// Listens for <see cref="PlayerMovementMessage"/> messages from the network and updates the velocity of the corresponding player entity.
    /// </summary>
    public class PlayerMovementListener : IInitializable, IDisposable
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly IMessageReceiver _messageReceiver;
        private readonly ILogger _logger;
        private IDisposable? _subscription;

        public PlayerMovementListener(EntityRegistry entityRegistry, IMessageReceiver messageReceiver, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _messageReceiver = messageReceiver;
            _logger = logger;
        }

        public void Initialize()
        {
            _subscription = _messageReceiver.RegisterMessageHandler<PlayerMovementMessage>(GetType().Name, HandlePlayerMovementMessage);
        }

        /// <summary>
        /// Handles incoming <see cref="PlayerMovementMessage"/> messages and applies movement to the corresponding player entity.
        /// </summary>
        /// <param name="peerId">The network peer ID of the sender.</param>
        /// <param name="msg">The movement message containing input data.</param>
        private void HandlePlayerMovementMessage(int peerId, PlayerMovementMessage msg)
        {
            // sanity check for message value
            if (msg.MoveDirection == Vector2.Zero)
            {
                _logger.Warn(LoggedFeature.Input, $"Received PlayerMovementMessage with zero movement from peer {peerId}. Ignoring.");
                return;
            }

            // Get the local player entity by peer ID
            var entity = _entityRegistry
                .GetAll()
                .Where(x => x.Has<PeerComponent>())
                .Where(x => x.Has<PlayerTagComponent>())
                .FirstOrDefault(x => x.GetRequired<PeerComponent>().PeerId == peerId);

            if (entity == null)
            {
                _logger.Warn(LoggedFeature.Input, $"Received PlayerMovementMessage from peer {peerId} but no player entity found.");
                return;
            }

            var moveDirection = new Vector3(msg.MoveDirection.X, 0, msg.MoveDirection.Y);
            var velocity = moveDirection * InputConstants.PlayerSpeed;

            entity.AddOrReplaceComponent(new VelocityComponent { Value = velocity });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}