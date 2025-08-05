using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
using Shared.Networking.Messages;
using Shared.Scheduling;
using UnityEngine;

namespace Core.Input
{
    public class InputListener : ITickable, IInputListener
    {
        private readonly TickSync _tickSync;
        private readonly IMessageSender _messageSender;
        private readonly Dictionary<uint, PlayerMovementMessage> _inputBuffer = new();
        private Vector2 _lastSentMovement;

        public InputListener(TickSync tickSync, IMessageSender messageSender)
        {
            _tickSync = tickSync;
            _messageSender = messageSender;
        }

        public bool TryGetMovementAtTick(uint tick, out PlayerMovementMessage input) => _inputBuffer.TryGetValue(tick, out input);

        public void Tick()
        {
            var move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical")).normalized;
            var tick = _tickSync.ClientTick;

            var input = new PlayerMovementMessage
            {
                ClientTick = tick,
                MoveDirection = move.ToNumericsVector2()
            };
            
            // Store the input in the buffer for client-side prediction.
            _inputBuffer[tick] = input;

            // Only send an update to the server if the input state has actually changed.
            if (move == _lastSentMovement) return;
            
            // Send the new input state to the server.
            _messageSender.SendMessageToServer(MessageType.PlayerMovement, input);

            // Update the last sent state.
            _lastSentMovement = move;
        }
    }
}