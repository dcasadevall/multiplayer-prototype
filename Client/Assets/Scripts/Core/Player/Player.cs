using System;
using Core.Input;
using UnityEngine;

namespace Core.Player
{
    public delegate void OnShootHandler(Vector3 origin, Vector3 direction);

    public class Player : IDisposable, IPlayer
    {
        private const float MovementSpeed = 4f;
        private const float RotationSpeed = 0.25f;

        public event OnShootHandler OnShoot;

        private readonly IInputListener _inputListener;
        private Vector3 _lastMovementDirection;

        public string Id { get; }
        public Vector3 Position { get; private set; } = new(0, 1, 0);
        public Quaternion Rotation { get; private set; }

        public Player(string id, IInputListener inputListener)
        {
            Id = id;
            _inputListener = inputListener;
            _inputListener.OnShoot += HandleShoot;
        }

        private void HandleShoot() => OnShoot?.Invoke(Position, _lastMovementDirection);

        public void Tick()
        {
            Vector2 input = _inputListener.Movement.normalized;
            Vector3 movement = new Vector3(input.x, 0, input.y) * (MovementSpeed * Time.fixedDeltaTime);
            Position += movement;

            if (movement == Vector3.zero)
            {
                return;
            }

            _lastMovementDirection = movement;

            Rotation = Quaternion.Slerp(
                Rotation,
                Quaternion.LookRotation(movement),
                RotationSpeed
            );
        }

        public void Dispose()
        {
            _inputListener.OnShoot -= HandleShoot;
        }
    }
}
