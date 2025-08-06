namespace Shared.ECS.Simulation
{
    public enum WorldMode
    {
        /// <summary>
        /// Server mode is used for the authoritative game server.
        /// The world tick is advanced incrementally.
        /// </summary>
        Server,

        /// <summary>
        /// The client mode is used for the game clients.
        /// The world tick is synchronized with the server tick,
        /// </summary>
        Client
    }
}