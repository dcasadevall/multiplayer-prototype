namespace Shared.ECS.TickSync
{
    public interface ITickSync
    {
        /// <summary>
        /// Server tick number, replicated from the server.
        /// </summary>
        public uint ServerTick { get; }

        /// <summary>
        /// Client tick number. Should be ahead of the server tick by a few ticks
        /// for client-side prediction.
        /// </summary>
        public uint ClientTick { get; }

        /// <summary>
        /// Server tick number smoothed over time.
        /// Used for interpolation in visuals.
        /// </summary>
        public float SmoothedTick { get; set; }
    }
}