using System;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using Shared;
using Shared.Networking;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Unity-specific implementation of IMessageReceiver for handling network messages.
    /// 
    /// <para>
    /// This class provides a bridge between Unity and the networking layer, allowing
    /// the client to receive and process messages from the server. It can be integrated
    /// with various networking libraries like LiteNetLib.
    /// </para>
    /// </summary>
    public class UnityMessageReceiver : MonoBehaviour, IMessageReceiver
    {
        [SerializeField] 
        private UnityServiceProvider _serviceProvider;
        
        public event Action<MessageType, byte[]> OnMessageReceived;
        
        private CancellationTokenSource _cancellationTokenSource;

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
            Debug.Log($"UnityMessageReceiver: Received {messageType} message of {data.Length} bytes");
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
            // TODO: Implement sending logic
            // This is where you would send the message to the server
            // using your chosen networking library
        }
        
        private void OnDestroy()
        {
            StopListening();
        }
    }
} 