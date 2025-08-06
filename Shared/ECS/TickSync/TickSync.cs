namespace Shared.ECS.TickSync
{
    /// <summary>
    /// TickSync provides a way to synchronize ticks between server and client.
    /// </summary>
    public class TickSync : ITickSync
    {
        /// <summary>
        /// Server tick number, replicated from the server.
        /// </summary>
        public uint ServerTick { get; set; }

        /// <summary>
        /// Client tick number. Should be ahead of the server tick by a few ticks
        /// for client-side prediction.
        /// </summary>
        public uint ClientTick { get; set; }

        /// <summary>
        /// Server tick number smoothed over time.
        /// Used for interpolation in visuals.
        /// </summary>
        public float SmoothedTick { get; set; }
    }
}