// using System.Text.Json;
// using Shared.Logging;
// using Shared.Networking;
// using Shared.Networking.Messages;
// using UnityEngine;
//
// namespace Core
// {
//     /// <summary>
//     /// Unity implementation of IMessageSender that sends messages through UnityMessageReceiver.
//     /// </summary>
//     public class UnityMessageSender : IMessageSender
//     {
//         private readonly UnityMessageReceiver _messageReceiver;
//         private readonly ILogger _logger;
//
//         public UnityMessageSender(UnityMessageReceiver messageReceiver, ILogger logger)
//         {
//             _messageReceiver = messageReceiver;
//             _logger = logger;
//         }
//
//         public void SendMessage(ClientToServerMessage message)
//         {
//             try
//             {
//                 // Serialize the message to JSON
//                 var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
//                 {
//                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//                 });
//
//                 var messageBytes = System.Text.Encoding.UTF8.GetBytes(json);
//
//                 _logger.Debug("Sending message to server: {0}", message.Type);
//
//                 // Send through the message receiver
//                 _messageReceiver.SendToServer(messageBytes);
//             }
//             catch (System.Exception ex)
//             {
//                 _logger.Error("Failed to send message to server: {0}", ex.Message);
//             }
//         }
//     }
// } 