using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Shared;
using Shared.Networking;
using Shared.Scheduling;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Implementation of IMessageReceiver for handling network messages.
    /// 
    /// <para>
    /// This class provides a bridge between Unity and the networking layer, allowing
    /// the client to receive and process messages from the server. It can be integrated
    /// with various networking libraries like LiteNetLib.
    /// </para>
    /// </summary>
    public class MessageReceiver : IMessageReceiver, IInitializable, IDisposable
    {
        private readonly EventBasedNetListener _netListener;
        public event Action<MessageType, byte[]> OnMessageReceived;
        
        private CancellationTokenSource _cancellationTokenSource;

        public MessageReceiver(EventBasedNetListener netListener)
        {
            _netListener = netListener;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Starts listening for incoming network messages.
        /// </summary>
        public void StartListening()
        {
            Debug.Log("UnityMessageReceiver: Started listening for network messages");
            var eventListener = _serviceProvider.GetService<EventBasedNetListener>();
            eventListener.NetworkReceiveEvent += HandleMessageReceived;
            
            // Connect to the server asynchronously
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ConnectToServer(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        
        /// <summary>
        /// Stops listening for incoming network messages.
        /// </summary>
        public void StopListening()
        {
            Debug.Log("UnityMessageReceiver: Stopped listening for network messages");
            var eventListener = _serviceProvider.GetService<EventBasedNetListener>();
            eventListener.NetworkReceiveEvent -= HandleMessageReceived;
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        
        /// <summary>
        /// Internal method to trigger message received events.
        /// This would typically be called by the networking library's message handler.
        /// </summary>
        private void HandleMessageReceived(NetPeer peer, 
            NetPacketReader reader, 
            byte channel, 
            DeliveryMethod deliveryMethod)
        {
            var messageType = (MessageType)reader.GetByte();
            var data = reader.GetRemainingBytes();
            OnMessageReceived?.Invoke(messageType, data);
        }

        /// <summary>
        /// Connects to the server at the specified address and port.
        /// </summary>
        /// <param name="token"></param>
        private void ConnectToServer(CancellationToken token)
        {
            var serverAddress = SharedConstants.ServerAddress;
            var port = SharedConstants.ServerPort;
            Debug.Log($"UnityMessageReceiver: Connecting to server at {serverAddress}:{port}");

            var client = _serviceProvider.GetService<NetManager>();
            client.Start();
            
            // Connect to the server
            Console.WriteLine("Connecting to localhost:9050...");
            client.Connect(serverAddress, port, SharedConstants.NetSecret);
            
            // --- The Client Loop ---
            try
            {
                // Send a message every second
                while (!token.IsCancellationRequested && client.IsRunning)
                {
                    client.PollEvents();
                
                    Thread.Sleep(10); // Wait 10ms
                }
            }
            finally
            {
                client.Stop();
            }
        }
        
        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="messageData">The message data to send.</param>
        public void SendToServer(byte[] messageData)
        {
            var client = _serviceProvider.GetService<NetManager>();
            if (!client.IsRunning)
            {
                Debug.LogWarning("UnityMessageReceiver: Cannot send message - client not running");
                return;
            }

            var peer = client.FirstPeer;
            if (peer == null)
            {
                Debug.LogWarning("UnityMessageReceiver: Cannot send message - no server connection");
                return;
            }

            // Create a writer and write the message type and data
            var writer = new NetDataWriter();
            writer.Put((byte)MessageType.Snapshot); // TODO: Add more message types as needed
            writer.Put(messageData);

            // Send to server
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            Debug.Log($"UnityMessageReceiver: Sent {messageData.Length} bytes to server");
        }
        
        private void OnDestroy()
        {
            StopListening();
        }
    }
} 