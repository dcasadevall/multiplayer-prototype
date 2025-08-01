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
        public event Action<MessageType, byte[]> OnMessageReceived;
        
        private CancellationTokenSource _cancellationTokenSource;
        
        /// <summary>
        /// Starts listening for incoming network messages.
        /// </summary>
        public void StartListening()
        {
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        
        /// <summary>
        /// Internal method to trigger message received events.
        /// This would typically be called by the networking library's message handler.
        /// </summary>
        /// <param name="messageType">The type of message received.</param>
        /// <param name="data">The message data.</param>
        private void HandleMessageReceived(MessageType messageType, byte[] data)
        {
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
            
            // TODO: Implement connection logic
            // This is where you would establish a connection to the server
            // using your chosen networking library
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
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
                
                    // If connected, send a message
                    if (client.FirstPeer != null && client.FirstPeer.ConnectionState == ConnectionState.Connected)
                    {
                        var writer = new NetDataWriter();
                        writer.Put($"Hello Server! Time is {DateTime.UtcNow:T}");
                        client.FirstPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                        Console.WriteLine("Sent message.");
                    }
                
                    Thread.Sleep(1000); // Wait 1 second
                }
            }
            finally
            {
                client.Stop();
            }
        }
        
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void DisconnectFromServer()
        {
            Debug.Log("UnityMessageReceiver: Disconnecting from server");
            
            // TODO: Implement disconnection logic
            // This is where you would cleanly disconnect from the server
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