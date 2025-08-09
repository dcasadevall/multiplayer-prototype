using System;
using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.TickSync;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Core.Input
{
    /// <summary>
    /// InputListener stores the input from the player and provides methods to retrieve the input at a specific tick.
    /// It uses buffers to store the past tick input.
    /// IT also handles the shooting input and raises an event when the player shoots.
    /// </summary>
    public class InputSystem : ISystem, IInputListener
    {
        public event Action OnShoot;

        // Buffers to store input for client-side prediction
        private readonly Dictionary<uint, Vector2> _movementInputBuffer = new();
        private readonly ITickSync _tickSync;

        public InputSystem(ITickSync tickSync)
        {
            _tickSync = tickSync;
        }
        
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            HandleMovementInput(tickNumber);
            HandleShotInput();
            RemoveStaleInputs(tickNumber);
        }
        
        public bool TryGetMovementAtTick(uint tick, out Vector2 moveDirection) => _movementInputBuffer.TryGetValue(tick, out moveDirection);

        /// <summary>
        /// Reads the current input and stores it in a buffer for client-side predictions
        /// </summary>
        /// <param name="tickNumber"></param>
        private void HandleMovementInput(uint tickNumber)
        {
            var move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical")).Normalized();
            _movementInputBuffer[tickNumber] = move;
        }

        private void HandleShotInput()
        {
            if (!UnityEngine.Input.GetKey(KeyCode.Space)) return;
            
            OnShoot?.Invoke();
        }

        private void RemoveStaleInputs(uint tickNumber)
        {
            // Remove inputs that are older than the current tick minus a certain threshold.
            // This is to prevent the buffer from growing indefinitely.
            var threshold = tickNumber - 60; 
            foreach (var tick in new List<uint>(_movementInputBuffer.Keys))
            {
                if (tick < threshold)
                {
                    _movementInputBuffer.Remove(tick);
                }
            }
        }
    }
}