// using System.Numerics;
// using Core;
// using Shared.ECS;
// using Shared.ECS.Simulation;
// using Shared.Logging;
// using UnityEngine;
//
// namespace Input
// {
//     /// <summary>
//     /// Handles player input for spawning and movement.
//     /// </summary>
//     [TickInterval(1)] // Update every frame for responsive input
//     public class PlayerInputSystem : ISystem
//     {
//         private readonly ILogger _logger;
//         private readonly PlayerPredictionSystem _playerPredictionSystem;
//         private bool _hasSpawned = false;
//
//         public PlayerInputSystem(ILogger logger, PlayerPredictionSystem playerPredictionSystem)
//         {
//             _logger = logger;
//             _playerPredictionSystem = playerPredictionSystem;
//         }
//
//         public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
//         {
//             HandleSpawnInput();
//             HandleMovementInput();
//         }
//
//         /// <summary>
//         /// Handles spawn input (Space key).
//         /// </summary>
//         private void HandleSpawnInput()
//         {
//             if (_hasSpawned) return;
//
//             if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
//             {
//                 _logger.Info("Space key pressed - requesting player spawn");
//                 
//                 // Spawn at origin for now
//                 var spawnPosition = new Vector3(0, 1, 0);
//                 _playerPredictionSystem.RequestPlayerSpawn(spawnPosition, "Player");
//                 
//                 _hasSpawned = true;
//             }
//         }
//
//         /// <summary>
//         /// Handles movement input (WASD keys).
//         /// </summary>
//         private void HandleMovementInput()
//         {
//             if (!_hasSpawned || _playerPredictionSystem.IsWaitingForSpawnConfirmation)
//             {
//                 return; // Don't handle movement until player is spawned
//             }
//
//             var direction = Vector3.Zero;
//
//             // Get input from Unity's Input system
//             if (UnityEngine.Input.GetKey(KeyCode.W))
//                 direction.Z += 1;
//             if (UnityEngine.Input.GetKey(KeyCode.S))
//                 direction.Z -= 1;
//             if (UnityEngine.Input.GetKey(KeyCode.A))
//                 direction.X -= 1;
//             if (UnityEngine.Input.GetKey(KeyCode.D))
//                 direction.X += 1;
//
//             // Normalize the direction vector
//             if (direction != Vector3.Zero)
//             {
//                 direction = Vector3.Normalize(direction);
//                 
//                 // Check if running (Shift key)
//                 var isRunning = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
//                 
//                 _playerPredictionSystem.SendMovementInput(direction, isRunning);
//             }
//         }
//     }
// } 