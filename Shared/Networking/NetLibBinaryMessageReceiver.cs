using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using Shared.Logging;
using Shared.Networking.Messages;
using Shared.Scheduling;

namespace Shared.Networking
{
    /// <summary>
    /// An <see cref="IMessageReceiver"/> implementation that integrates with LiteNetLib's event-based listener.
    /// Deserializes the incoming message using binary deserialization for messages that implement <see cref="INetSerializable"/>.
    /// </summary>
    public class NetLibBinaryMessageReceiver : IMessageReceiver, IInitializable, IDisposable
    {
        private readonly ILogger _logger;
        private readonly EventBasedNetListener _eventBasedNetListener;
        private readonly MessageFactory _messageFactory;
        private readonly Dictionary<Type, Dictionary<string, MessageHandler<object>>> _handlers = new();

        public NetLibBinaryMessageReceiver(EventBasedNetListener eventBasedNetListener,
            MessageFactory messageFactory,
            ILogger logger)
        {
            _logger = logger;
            _eventBasedNetListener = eventBasedNetListener;
            _messageFactory = messageFactory;
        }

        public void Initialize()
        {
            _eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        }

        public void Dispose()
        {
            _eventBasedNetListener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
        }

        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            NetworkStats.RecordMessageReceived(reader.UserDataSize);

            // Ensure the message type is valid
            var messageType = (MessageType)reader.GetByte();
            if (!Enum.IsDefined(typeof(MessageType), messageType))
            {
                _logger.Warn(LoggedFeature.Networking, "Received message with unknown type: {0}", messageType);
                return;
            }

            // Create the message instance using the factory
            var message = _messageFactory.Create(messageType);

            // Ensure the handler is registered for this message type
            var messageTypeClass = message.GetType();
            if (!_handlers.TryGetValue(messageTypeClass, out var handlers))
            {
                return;
            }

            try
            {
                message.Deserialize(reader);

                foreach (var handler in handlers.Values)
                    handler(peer.Id, message);
            }
            catch (Exception ex)
            {
                _logger.Error(LoggedFeature.Networking, "Error while processing message of type {0}: {1}", messageTypeClass.Name,
                    ex.Message);
            }
        }

        public IDisposable RegisterMessageHandler<TMessage>(string handlerId, MessageHandler<TMessage> handler)
        {
            var type = typeof(TMessage);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new Dictionary<string, MessageHandler<object>>();

            if (_handlers[type].ContainsKey(handlerId))
                _logger.Warn(LoggedFeature.Networking,
                    "Handler with ID '{0}' already registered for message type '{1}'. Overwriting existing handler.", handlerId, type.Name);

            _handlers[type][handlerId] = (peerId, message) => handler(peerId, (TMessage)message);
            return new HandlerRegistration(this, type, handlerId);
        }

        private sealed class HandlerRegistration : IDisposable
        {
            private readonly NetLibBinaryMessageReceiver _receiver;
            private readonly Type _messageType;
            private readonly string _handlerId;

            public HandlerRegistration(NetLibBinaryMessageReceiver receiver, Type messageType, string handlerId)
            {
                _receiver = receiver;
                _messageType = messageType;
                _handlerId = handlerId;
            }

            public void Dispose()
            {
                if (_receiver._handlers.TryGetValue(_messageType, out var handlers) && handlers.Remove(_handlerId))
                    _receiver._logger.Info(LoggedFeature.Networking, "Unregistered handler '{0}' for message type '{1}'.", _handlerId,
                        _messageType.Name);
            }
        }
    }
}