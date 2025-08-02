using System;
using System.Threading.Tasks;
using Shared.Networking;
using ILogger = Shared.Logging.ILogger;

namespace Core
{
    /// <summary>
    /// Manages the client's connection to the server, including the handshake process.
    /// </summary>
    public class ClientConnectionManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IMessageReceiver _messageReceiver;
        private readonly IMessageSender _messageSender;
        
        private TaskCompletionSource<Guid> _clientIdTask;
        private bool _isDisposed;

        public ClientConnectionManager(
            ILogger logger,
            IMessageReceiver messageReceiver,
            IMessageSender messageSender)
        {
            _logger = logger;
            _messageReceiver = messageReceiver;
            _messageSender = messageSender;
            
            _clientIdTask = new TaskCompletionSource<Guid>();
            _messageReceiver.OnMessageReceived += HandleMessageReceived;
        }

        /// <summary>
        /// Gets the client's unique ID assigned by the server.
        /// This will complete once the handshake process is finished.
        /// </summary>
        public Task<Guid> ClientId => _clientIdTask.Task;

        private void HandleMessageReceived(MessageType messageType, byte[] data)
        {
            if (messageType == MessageType.ClientIdAssignment)
            {
                try
                {
                    var message = System.Text.Json.JsonSerializer.Deserialize<ClientIdAssignmentMessage>(data);
                    if (message == null)
                    {
                        _logger.Error("Failed to deserialize ClientIdAssignmentMessage");
                        return;
                    }

                    _logger.Info("Received client ID: {0}", message.ClientId);
                    _clientIdTask.TrySetResult(message.ClientId);
                }
                catch (Exception e)
                {
                    _logger.Error("Error processing ClientIdAssignmentMessage: {0}", e);
                    _clientIdTask.TrySetException(e);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_messageReceiver != null)
            {
                _messageReceiver.OnMessageReceived -= HandleMessageReceived;
            }
        }
    }
}