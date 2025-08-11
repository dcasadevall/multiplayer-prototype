using System;

namespace Shared.Settings
{
    /// <summary>
    /// Network Settings are used in client as ScriptableObject too, so they need to be serializable.
    /// </summary>
    [Serializable]
    public class NetworkSettings
    {
        /// <summary>
        /// Server address for the game server.
        /// In a real application, this would be configurable and not hardcoded (or shared).
        /// </summary>
        public string ServerAddress = "localhost";

        /// <summary>
        /// Port number for the server to listen on.
        /// In a real application, this should be configurable and not hardcoded,
        /// but for simplicity, we use a constant here.
        /// </summary>
        public int ServerPort = 9050;

        /// <summary>
        /// Secret key used to connect to the server.
        /// This would be stored as a deployment secret in a real application.
        /// </summary>
        public string NetSecret = "your-secret-key";
    }
}