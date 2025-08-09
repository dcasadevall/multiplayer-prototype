using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Shared.ECS.Replication;
using Shared.Input;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// A factory for creating message instances from a message type.
    /// This is used to avoid the performance overhead of Activator.CreateInstance.
    /// </summary>
    public class MessageFactory
    {
        private readonly Dictionary<MessageType, Func<INetSerializable>> _constructors = new();

        /// <summary>
        /// Constructs the factory with all known message types.
        /// </summary>
        public MessageFactory(IComponentSerializer componentSerializer)
        {
            _constructors[MessageType.Connected] = () => new ConnectedMessage();
            _constructors[MessageType.PlayerMovement] = () => new PlayerMovementMessage();
            _constructors[MessageType.PlayerShot] = () => new PlayerShotMessage();
            _constructors[MessageType.Delta] = () => new WorldDeltaMessage(componentSerializer);
        }

        /// <summary>
        /// Creates a new message instance for the given message type.
        /// </summary>
        /// <param name="type">The type of the message to create.</param>
        /// <returns>A new message instance.</returns>
        /// <exception cref="Exception">Thrown if the message type is unknown.</exception>
        public INetSerializable Create(MessageType type)
        {
            if (!_constructors.TryGetValue(type, out var constructor))
            {
                throw new Exception($"Unknown message type: {type}");
            }

            return constructor.Invoke();
        }
    }
}