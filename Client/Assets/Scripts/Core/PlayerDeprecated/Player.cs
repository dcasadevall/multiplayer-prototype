using System;
using Core.Input;
using UnityEngine;

namespace Core.PlayerDeprecated
{
    public delegate void OnShootHandler(Vector3 origin, Vector3 direction);

    public class Player : IDisposable, IPlayer
    {
        private const float MovementSpeed = 4f;
        private const float RotationSpeed = 0.25f;

        public event OnShootHandler OnShoot;

        private readonly IInputListener _inputListener;
        private Vector3 _lastMovementDirection;

        public Vector3 Position { get; private set; } = new(0, 1, 0);
        public Quaternion Rotation { get; private set; }

        public Player(IInputListener inputListener)
        {
            _inputListener = inputListener;
        }

        private void HandleShoot() => OnShoot?.Invoke(Position, _lastMovementDirection);

        public void Tick()
        {
            // Get input
            // Vector2 input = _inputListener.Movement.normalized;
        
            // // Send input to server
            // var inputCommand = new PlayerInputCommand { Movement = new System.Numerics.Vector2(input.x, input.y) };
            // _messageSender.SendMessage(0, MessageType.PlayerInput, inputCommand, ChannelType.Unreliable);
            
            // Apply movement locally for prediction
            // Vector3 movement = new Vector3(input.x, 0, input.y) * (MovementSpeed * Time.fixedDeltaTime);
            // Position += movement;
            //
            // if (movement == Vector3.zero)
            // {
            //     return;
            // }
            //
            // _lastMovementDirection = movement;
            //
            // Rotation = Quaternion.Slerp(
            //     Rotation,
            //     Quaternion.LookRotation(movement),
            //     RotationSpeed
            // );
        }

        public void Dispose()
        {
            // _inputListener.OnShoot -= HandleShoot;
        }
    }
}
