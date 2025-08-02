using System;
using System.Threading.Tasks;

namespace Shared.Networking
{
    /// <summary>
    /// Represents a network client capable of connecting to a remote server.
    /// </summary>
    public interface INetworkingClient
    {
        /// <summary>
        /// Initiates an asynchronous connection to the server. This method is thread-safe.
        /// One should be able to call this method multiple times to connect to different servers or retry connections.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="port">The server port.</param>
        /// <param name="netSecret">The connection secret.</param>
        /// <param name="timeoutSeconds">Timeout in seconds for the connection attempt.</param>
        /// <returns>
        /// A Task that completes with an <see cref="IDisposable"/> connection handle, or throws on failure.
        /// </returns>
        Task<IDisposable> ConnectAsync(string address, int port, string netSecret = "", int timeoutSeconds = 10);
    }
}