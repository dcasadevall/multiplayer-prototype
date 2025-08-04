using Shared.Input;

namespace Core.Input
{
    public interface IInputListener
    {
        public bool TryGetMovementAtTick(uint tick, out PlayerMovementMessage input);
    }
}