using System;
using System.Linq;
using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Logging;
using Shared.Networking;
using Shared.Scheduling;

namespace Shared.Input
{
    /// <summary>
    /// Listens for <see cref="PlayerMovementMessage"/> messages from the network and applies movement to the corresponding player entity.
    /// 
    /// <para>
    /// This class subscribes to player movement messages received from remote clients (via <see cref="IMessageReceiver"/>).
    /// When a message is received, it finds the player entity associated with the sending peer and updates its <see cref="PositionComponent"/>
    /// based on the movement input contained in the message.
    /// </para>
    /// 
    /// <para>
    /// This listener should be registered on the server or authoritative simulation, where it can safely mutate entity state.
    /// </para>
    /// </summary>
    public class PlayerMovementListener : IInitializable, IDisposable
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly IMessageReceiver _messageReceiver;
        private readonly ILogger _logger;
        private IDisposable? _subscription;

        /// <summary>
        /// Constructs a new <see cref="PlayerMovementListener"/>.
        /// </summary>
        /// <param name="entityRegistry">The entity registry containing all entities.</param>
        /// <param name="messageReceiver">The message receiver for network messages.</param>
        /// <param name="logger">Logger for warnings and diagnostics.</param>
        public PlayerMovementListener(EntityRegistry entityRegistry, IMessageReceiver messageReceiver, ILogger logger)
        {
            _entityRegistry = entityRegistry;
            _messageReceiver = messageReceiver;
            _logger = logger;
        }

        /// <summary>
        /// Registers the listener to handle <see cref="PlayerMovementMessage"/> messages.
        /// </summary>
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
            var movement = new Vector3(msg.MoveDirection.X, 0, msg.MoveDirection.Y) * 0.1f;
            if (movement == Vector3.Zero)
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

            var position = entity.GetRequired<PositionComponent>().Value;

            entity.AddOrReplaceComponent(new PositionComponent { Value = position + movement });
        }

        /// <summary>
        /// Unregisters the message handler and releases resources.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}