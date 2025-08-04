namespace Shared.ECS.TickSync
{
    /// <summary>
    /// TickSync provides a way to synchronize ticks between server and client.
    /// </summary>
    public class TickSync
    {
        /// <summary>
        /// Server tick number, replicated from the server.
        /// </summary>
        public uint ServerTick { get; internal set; }

        /// <summary>
        /// Client tick number. Should be ahead of the server tick by a few ticks
        /// for client-side prediction.
        /// </summary>
        public uint ClientTick { get; internal set; }

        /// <summary>
        /// Server tick number smoothed over time.
        /// Used for interpolation in visuals.
        /// </summary>
        public float SmoothedTick { get; internal set; }

        /// <summary>
        /// The offset in ticks to the server tick.
        /// </summary>
        public int TickOffset { get; internal set; }

        /// <summary>
        /// Used to determine if the TickSync has been initialized.
        /// </summary>
        internal bool IsInitialized { get; set; }
    }
}