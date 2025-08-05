namespace Shared.Input
{
    public static class InputConstants
    {
        private const float Speed = 5.0f;
        private const float TickRate = 60.0f;

        public const float MoveDeltaPerTick = Speed / TickRate;
    }
}