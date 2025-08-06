using System;
using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS.TickSync;
using Shared.Scheduling;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Core.Input
{
    /// <summary>
    /// InputListener stores the input from the player and provides methods to retrieve the input at a specific tick.
    /// It uses buffers to store the past tick input.
    /// IT also handles the shooting input and raises an event when the player shoots.
    /// </summary>
    public class InputListener : ITickable, IInputListener
    {
        public event Action OnShoot;

        // Buffers to store input for client-side prediction
        private readonly Dictionary<uint, Vector2> _movementInputBuffer = new();
        private readonly ITickSync _tickSync;

        public InputListener(ITickSync tickSync)
        {
            _tickSync = tickSync;
        }

        public bool TryGetMovementAtTick(uint tick, out Vector2 moveDirection) => _movementInputBuffer.TryGetValue(tick, out moveDirection);

        public void Tick()
        {
            HandleMovementInput();
            HandleShotInput();
            RemoveStaleInputs();
        }

        private void HandleMovementInput()
        {
            var move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical")).Normalized();
            var tick = _tickSync.ClientTick;

            // Store the input in the buffer for client-side prediction.
            _movementInputBuffer[tick] = move;
        }

        private void HandleShotInput()
        {
            if (!UnityEngine.Input.GetKey(KeyCode.Space)) return;
            
            OnShoot?.Invoke();
        }

        private void RemoveStaleInputs()
        {
            // Remove inputs that are older than the current tick minus a certain threshold.
            // This is to prevent the buffer from growing indefinitely.
            var threshold = _tickSync.ClientTick - 60; 
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