using System.Numerics;
using Shared.Input;

namespace Core.Input
{
    public interface IInputListener
    {
        public bool TryGetShotAtTick(uint tick, out Vector3 shotDirection);
        
        public bool TryGetMovementAtTick(uint tick, out Vector2 moveDirection);
    }
}