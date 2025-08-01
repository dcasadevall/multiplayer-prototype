using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
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
        
        private bool _isListening = false;
        
        /// <summary>
        /// Starts listening for incoming network messages.
        /// </summary>
        public void StartListening()
        {
            if (_isListening)
            {
                Debug.LogWarning("UnityMessageReceiver: Already listening for messages");
                return;
            }
            
            _isListening = true;
            Debug.Log("UnityMessageReceiver: Started listening for network messages");
            
            // TODO: Initialize networking library (e.g., LiteNetLib)
            // This is where you would set up the network connection and start
            // listening for incoming messages from the server
        }
        
        /// <summary>
        /// Stops listening for incoming network messages.
        /// </summary>
        public void StopListening()
        {
            if (!_isListening)
            {
                Debug.LogWarning("UnityMessageReceiver: Not currently listening for messages");
                return;
            }
            
            _isListening = false;
            Debug.Log("UnityMessageReceiver: Stopped listening for network messages");
            
            // TODO: Clean up networking library resources
            // This is where you would disconnect from the server and clean up
            // any network-related resources
        }
        
        /// <summary>
        /// Internal method to trigger message received events.
        /// This would typically be called by the networking library's message handler.
        /// </summary>
        /// <param name="messageType">The type of message received.</param>
        /// <param name="data">The message data.</param>
        internal void HandleMessageReceived(MessageType messageType, byte[] data)
        {
            if (!_isListening)
            {
                Debug.LogWarning("UnityMessageReceiver: Received message while not listening");
                return;
            }
            
            Debug.Log($"UnityMessageReceiver: Received {messageType} message of {data.Length} bytes");
            OnMessageReceived?.Invoke(messageType, data);
        }
        
        /// <summary>
        /// Connects to the server at the specified address and port.
        /// </summary>
        /// <param name="serverAddress">The server's IP address or hostname.</param>
        /// <param name="port">The server's port number.</param>
        public void ConnectToServer(string serverAddress, int port)
        {
            Debug.Log($"UnityMessageReceiver: Connecting to server at {serverAddress}:{port}");
            
            // TODO: Implement connection logic
            // This is where you would establish a connection to the server
            // using your chosen networking library
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            
            // Connect to the server
            Console.WriteLine("Connecting to localhost:9050...");
            client.Connect("localhost", 9050, "");
            
            // --- The Client Loop ---
            try
            {
                // Send a message every second
                while (true)
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
            if (!_isListening)
            {
                Debug.LogWarning("UnityMessageReceiver: Cannot send message while not connected");
                return;
            }
            
            Debug.Log($"UnityMessageReceiver: Sending message of {messageData.Length} bytes to server");
            
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