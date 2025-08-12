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
        /// </summary>
        public string ServerAddress = "127.0.0.1";

        /// <summary>
        /// Port number for the server to listen on.
        /// In a real application, this should be configurable and not hardcoded,
        /// but for simplicity, we use a constant here.
        /// </summary>
        public int ServerPort = 8080;

        /// <summary>
        /// Secret key used to connect to the server.
        /// This would be stored as a deployment secret in a real application.
        /// </summary>
        public string NetSecret = "your-secret-key";
    }
}