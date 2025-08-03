using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using LiteNetLib;
using Shared.Logging;
using Shared.Scheduling;

namespace Shared.Networking
{
    /// <summary>
    /// An <see cref="IMessageReceiver"/> implementation that integrates with LiteNetLib's event-based listener.
    /// Deserializes the incoming message using Json.
    /// 
    /// <para>
    /// <see cref="NetLibJsonMessageReceiver"/> listens for incoming network messages using <see cref="EventBasedNetListener"/>,
    /// parses the message type, and dispatches the message payload to registered handlers based on message type.
    /// Handlers can be registered and unregistered dynamically, and are identified by a unique handler ID.
    /// </para>
    /// <para>
    /// This class also implements <see cref="IInitializable"/> and <see cref="IDisposable"/> to manage event subscription lifecycle.
    /// </para>
    /// <para>
    /// All received messages are logged, and errors in handler invocation are caught and logged.
    /// </para>
    /// </summary>
    public class NetLibJsonMessageReceiver : IMessageReceiver, IInitializable, IDisposable
    {
        private readonly ILogger _logger;
        private readonly EventBasedNetListener _eventBasedNetListener;

        // Maps message type to a dictionary of handlerId -> handler delegate
        private Dictionary<Type, Dictionary<string, Action<object>>> _handlers = new();

        /// <summary>
        /// Constructs a new <see cref="NetLibJsonMessageReceiver"/>.
        /// </summary>
        /// <param name="logger">Logger for structured logging of message events and errors.</param>
        /// <param name="eventBasedNetListener">The LiteNetLib event listener to subscribe to.</param>
        public NetLibJsonMessageReceiver(ILogger logger, EventBasedNetListener eventBasedNetListener)
        {
            _logger = logger;
            _eventBasedNetListener = eventBasedNetListener;
        }

        /// <summary>
        /// Subscribes to the network receive event.
        /// </summary>
        public void Initialize()
        {
            _eventBasedNetListener.NetworkReceiveEvent += OnNetworkReceiveEvent;
        }

        /// <summary>
        /// Unsubscribes from the network receive event.
        /// </summary>
        public void Dispose()
        {
            _eventBasedNetListener.NetworkReceiveEvent -= OnNetworkReceiveEvent;
        }

        /// <summary>
        /// Handles incoming network messages, dispatching them to registered handlers by message type.
        /// </summary>
        private void OnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel,
            DeliveryMethod deliveryMethod)
        {
            // Read the message type from the packet
            _logger.Debug("Received message from peer {0} on channel {1} with delivery method {2}",
                peer.Id, channel, deliveryMethod);

            var messageType = (MessageType)reader.GetByte();
            var messageTypeClass = MessageTypeMap.GetMessageType(messageType);
            if (messageTypeClass == null)
            {
                _logger.Warn("Received message with unknown type: {0}", messageType);
                return;
            }

            // Read the rest of the data into a byte array
            var data = reader.GetRemainingBytes();
            if (!_handlers.TryGetValue(messageTypeClass, out var handlers))
            {
                _logger.Warn("No handlers registered for message type: {0}", messageTypeClass.Name);
                return;
            }

            object? message = null;
            var json = Encoding.UTF8.GetString(data);
            try
            {
                // Deserialize the JSON data into the appropriate message type
                _logger.Debug("Deserializing message of type {0}: {1}", messageTypeClass.Name, json);
                message = JsonSerializer.Deserialize(json, messageTypeClass);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to deserialize message of type {0}: {1}", messageTypeClass.Name, ex.Message);
                return;
            }

            if (message == null)
            {
                _logger.Error("Unable to deserialize message type {0}", json, messageTypeClass.Name);
                return;
            }

            // Invoke all registered handlers for this message type
            foreach (var handler in handlers.Values)
            {
                try
                {
                    _logger.Debug("Invoking handler for message type {0}", messageTypeClass.Name);
                    handler(message);
                }
                catch (Exception ex)
                {
                    _logger.Error("Error while processing message of type {0}: {1}", messageTypeClass.Name, ex.Message);
                }
            }
        }

        /// <inheritdoc />
        public IDisposable RegisterMessageHandler<TMessage>(string handlerId, Action<TMessage> handler)
        {
            var type = typeof(TMessage);
            if (!_handlers.ContainsKey(type))
            {
                _handlers[type] = new Dictionary<string, Action<object>>();
            }

            if (_handlers[type].ContainsKey(handlerId))
            {
                _logger.Warn("Handler with ID '{0}' already registered for message type '{1}'." +
                             " Overwriting existing handler.", handlerId, type.Name);
            }

            _handlers[type][handlerId] = message => handler((TMessage)message);
            return new HandlerRegistration(this, type, handlerId);
        }

        /// <summary>
        /// Helper class for unregistering a handler when disposed.
        /// </summary>
        private sealed class HandlerRegistration : IDisposable
        {
            private readonly NetLibJsonMessageReceiver _receiver;
            private readonly Type _messageType;
            private readonly string _handlerId;

            public HandlerRegistration(NetLibJsonMessageReceiver receiver, Type messageType, string handlerId)
            {
                _receiver = receiver;
                _messageType = messageType;
                _handlerId = handlerId;
            }

            public void Dispose()
            {
                if (_receiver._handlers.TryGetValue(_messageType, out var handlers) &&
                    handlers.Remove(_handlerId))
                {
                    _receiver._logger.Info("Unregistered handler '{0}' for message type '{1}'.", _handlerId,
                        _messageType.Name);
                }
            }
        }
    }
}