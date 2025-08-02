// using System;
// using System.Numerics;
// using Shared.ECS;
// using Shared.ECS.Components;
// using Shared.ECS.Simulation;
// using Shared.Logging;
// using Shared.Networking.Messages;
// using UnityEngine;
//
// namespace Core
// {
//     /// <summary>
//     /// ECS system that handles client-side prediction for the local player.
//     /// 
//     /// <para>
//     /// This system creates a predicted player entity when a spawn request is sent,
//     /// and keeps it in sync with the server's authoritative version. It also handles
//     /// input prediction for smooth client-side movement.
//     /// </para>
//     /// </summary>
//     [TickInterval(1)] // Update every frame for smooth prediction
//     public class PlayerPredictionSystem : ISystem
//     {
//         private readonly ILogger _logger;
//         private readonly IMessageSender _messageSender;
//         private Entity? _predictedPlayerEntity;
//         private Guid _localPlayerEntityId = Guid.Empty;
//         private bool _waitingForSpawnConfirmation = false;
//         private Vector3 _pendingSpawnPosition;
//         private long _spawnRequestTimestamp;
//
//         public PlayerPredictionSystem(ILogger logger, IMessageSender messageSender)
//         {
//             _logger = logger;
//             _messageSender = messageSender;
//         }
//
//         /// <summary>
//         /// Requests to spawn a player at the specified position.
//         /// </summary>
//         /// <param name="position">The desired spawn position.</param>
//         /// <param name="playerName">Optional player name.</param>
//         public void RequestPlayerSpawn(Vector3 position, string playerName = "")
//         {
//             if (_waitingForSpawnConfirmation)
//             {
//                 _logger.Warn("Already waiting for spawn confirmation, ignoring new request");
//                 return;
//             }
//
//             _logger.Info("Requesting player spawn at position {0}", position);
//
//             // Create spawn request message
//             var spawnRequest = new PlayerSpawnRequest(position, playerName);
//             _spawnRequestTimestamp = spawnRequest.Timestamp;
//             _pendingSpawnPosition = position;
//
//             // Send the request to the server
//             _messageSender.SendMessage(spawnRequest);
//
//             // Create predicted entity immediately for responsive feel
//             CreatePredictedPlayerEntity(position);
//
//             _waitingForSpawnConfirmation = true;
//         }
//
//         /// <summary>
//         /// Handles a spawn response from the server.
//         /// </summary>
//         /// <param name="response">The server's spawn response.</param>
//         public void HandleSpawnResponse(PlayerSpawnResponse response)
//         {
//             if (!_waitingForSpawnConfirmation)
//             {
//                 _logger.Warn("Received spawn response but not waiting for confirmation");
//                 return;
//             }
//
//             _waitingForSpawnConfirmation = false;
//
//             if (response.Success)
//             {
//                 _localPlayerEntityId = response.PlayerEntityId;
//                 _logger.Info("Player spawn confirmed by server. Entity ID: {0}", _localPlayerEntityId);
//
//                 // Update predicted entity with server's position if it differs
//                 if (_predictedPlayerEntity != null)
//                 {
//                     var positionComponent = _predictedPlayerEntity.Get<PositionComponent>();
//                     if (positionComponent != null)
//                     {
//                         var serverPosition = response.SpawnPosition;
//                         var currentPosition = positionComponent.Value;
//
//                         // If server position differs significantly, snap to it
//                         if (Vector3.Distance(currentPosition, serverPosition) > 0.1f)
//                         {
//                             positionComponent.X = serverPosition.X;
//                             positionComponent.Y = serverPosition.Y;
//                             positionComponent.Z = serverPosition.Z;
//                             _logger.Debug("Snapped predicted player to server position: {0}", serverPosition);
//                         }
//                     }
//                 }
//             }
//             else
//             {
//                 _logger.Error("Player spawn failed: {0}", response.ErrorMessage);
//                 
//                 // Remove predicted entity since spawn failed
//                 if (_predictedPlayerEntity != null)
//                 {
//                     // Note: In a real implementation, you'd need to remove it from the registry
//                     _predictedPlayerEntity = null;
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Sends movement input to the server.
//         /// </summary>
//         /// <param name="direction">The movement direction vector.</param>
//         /// <param name="isRunning">Whether the player is running.</param>
//         public void SendMovementInput(Vector3 direction, bool isRunning = false)
//         {
//             if (_localPlayerEntityId == Guid.Empty)
//             {
//                 return; // No local player yet
//             }
//
//             var movementInput = new PlayerMovementInput(direction, isRunning);
//             _messageSender.SendMessage(movementInput);
//
//             // Apply input to predicted entity for immediate feedback
//             if (_predictedPlayerEntity != null)
//             {
//                 ApplyMovementToPredictedEntity(direction, isRunning);
//             }
//         }
//
//         public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
//         {
//             // Handle any pending spawn confirmations or other updates
//             // This could include reconciliation with server state
//         }
//
//         /// <summary>
//         /// Creates a predicted player entity for immediate client-side feedback.
//         /// </summary>
//         /// <param name="position">The spawn position.</param>
//         private void CreatePredictedPlayerEntity(Vector3 position)
//         {
//             // In a real implementation, you'd create this through the registry
//             // For now, we'll just log that we would create it
//             _logger.Debug("Creating predicted player entity at position {0}", position);
//
//             // TODO: Create entity through registry with predicted components
//             // This would include PositionComponent, VelocityComponent, HealthComponent, etc.
//             // and mark it as a predicted entity
//         }
//
//         /// <summary>
//         /// Applies movement input to the predicted entity for immediate feedback.
//         /// </summary>
//         /// <param name="direction">The movement direction.</param>
//         /// <param name="isRunning">Whether the player is running.</param>
//         private void ApplyMovementToPredictedEntity(Vector3 direction, bool isRunning)
//         {
//             if (_predictedPlayerEntity == null) return;
//
//             var velocityComponent = _predictedPlayerEntity.Get<VelocityComponent>();
//             if (velocityComponent != null)
//             {
//                 var speed = isRunning ? 10f : 5f; // Adjust speeds as needed
//                 velocityComponent.X = direction.X * speed;
//                 velocityComponent.Y = direction.Y * speed;
//                 velocityComponent.Z = direction.Z * speed;
//             }
//         }
//
//         /// <summary>
//         /// Gets the local player entity ID.
//         /// </summary>
//         public Guid LocalPlayerEntityId => _localPlayerEntityId;
//
//         /// <summary>
//         /// Gets whether we're currently waiting for a spawn confirmation.
//         /// </summary>
//         public bool IsWaitingForSpawnConfirmation => _waitingForSpawnConfirmation;
//     }
// } 