using System.Collections.Generic;
using Core.MathUtils;
using Shared.ECS.TickSync;
using Shared.Input;
using Shared.Networking;
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

            if (move == _lastSentMovement)
            {
                // No change in input, don't send anything
                return;
            }
            
            var tick = _tickSync.ClientTick;

            var input = new PlayerMovementMessage
            {
                ClientTick = tick,
                MoveDirection = move.ToNumericsVector2()
            };
            
            // Store the input in the buffer
            _inputBuffer[tick] = input;

            // Send to server
            _messageSender.SendMessageToServer(MessageType.PlayerMovement, input);
            _lastSentMovement = move;
        }
    }
}