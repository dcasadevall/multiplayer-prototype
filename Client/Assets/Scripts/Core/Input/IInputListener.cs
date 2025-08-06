using System;
using System.Numerics;
using Shared.Input;

namespace Core.Input
{
    public interface IInputListener
    {
        event Action OnShoot;
        
        public bool TryGetMovementAtTick(uint tick, out Vector2 moveDirection);
    }
}