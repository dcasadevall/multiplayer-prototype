using System;
using System.Collections.Generic;
using Shared.ECS.Replication;
using Shared.Input;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// MessageTypeMap is a static class that maps <see cref="MessageType"/> to their corresponding message types.
    /// This mapping is used to determine the type of message being sent or received over the network
    /// </summary>
    public static class MessageTypeMap
    {
        private static readonly Dictionary<MessageType, Type> _messageTypeMap = new()
        {
            { MessageType.Connected, typeof(ConnectedMessage) },
            { MessageType.Snapshot, typeof(WorldSnapshotMessage) },
            { MessageType.PlayerMovement, typeof(PlayerMovementMessage) },
            { MessageType.PlayerShot, typeof(PlayerShotMessage) },
        };

        /// <summary>
        /// Gets the type of the message associated with the given <see cref="MessageType"/>.
        /// </summary>
        /// <param name="messageType">The message type to look up.</param>
        /// <returns>
        /// The type of the message, or null if the message type is not registered.
        /// </returns>
        public static Type? GetMessageType(MessageType messageType)
        {
            return _messageTypeMap.GetValueOrDefault(messageType);
        }
    }
}