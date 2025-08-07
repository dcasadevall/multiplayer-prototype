using System;

namespace Shared
{
    /// <summary>
    /// This class contains shared constants used across the application.
    /// </summary>
    public static class SharedConstants
    {
        /// <summary>
        /// Server address for the game server.
        /// In a real application, this would be configurable and not hardcoded (or shared).
        /// </summary>
        public static string ServerAddress { get; } = "localhost";

        /// <summary>
        /// Port number for the server to listen on.
        /// In a real application, this should be configurable and not hardcoded,
        /// but for simplicity, we use a constant here.
        /// </summary>
        public static int ServerPort { get; } = 9050;

        /// <summary>
        /// Secret key used to connect to the server.
        /// This would be stored as a deployment secret in a real application.
        /// </summary>
        public static string NetSecret { get; } = "your-secret-key";

        /// <summary>
        /// Tick rate for the world simulation.
        /// This defines how often the world updates its state.
        /// </summary>
        public static int WorldTicksPerSecond { get; } = 30;

        /// <summary>
        /// The fixed delta time for the world simulation.
        /// </summary>
        public static TimeSpan FixedDeltaTime { get; } = TimeSpan.FromSeconds(1.0f / WorldTicksPerSecond);
    }
}