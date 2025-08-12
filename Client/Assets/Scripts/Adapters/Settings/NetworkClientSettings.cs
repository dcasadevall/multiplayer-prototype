using System;
using UnityEngine;

namespace Adapters.Settings
{
    /// <summary>
    /// Because of how appsettings work on .net core, we need to create a separate class for the client
    /// if we want to serialize it as a ScriptableObject.
    /// </summary>
    [Serializable]
    public class NetworkClientSettings
    {
        /// <summary>
        /// Server address for the game server.
        /// Defaults to 0.0.0.0 to listen on all available network interfaces, which is required for containerized deployments.
        /// For fly.io, you need to use "fly-global-services" on the server since,
        /// and [your-app-name].fly.dev on the client side.
        /// </summary>
        [SerializeField]
        private string _serverAddress = "multiplayer-prototype.fly.dev";
        public string ServerAddress => _serverAddress;

        /// <summary>
        /// Port number for the server to listen on.
        /// In a real application, this should be configurable and not hardcoded,
        /// but for simplicity, we use a constant here.
        /// </summary>
        [SerializeField]
        private int _serverPort = 9050;
        public int ServerPort => _serverPort;

        /// <summary>
        /// Secret key used to connect to the server.
        /// This would be stored as a deployment secret in a real application.
        /// </summary>
        [SerializeField]
        private string _netSecret = "your-secret-key"; 
        public string NetSecret => _netSecret;
    }
}