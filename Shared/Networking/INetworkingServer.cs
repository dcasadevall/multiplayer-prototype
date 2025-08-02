using System;

namespace Shared.Networking
{
    /// <summary>
    /// Defines an abstraction for a network server capable of accepting connections and handling network events.
    /// <para>
    /// Implementations of <see cref="INetworkingServer"/> are responsible for starting and stopping the server,
    /// listening for incoming connections, and managing the server's network lifecycle.
    /// </para>
    /// <para>
    /// The <see cref="StartServer"/> method should start the server on the specified address and port,
    /// and return an <see cref="IDisposable"/> that, when disposed, will stop the server and release all resources.
    /// </para>
    /// </summary>
    public interface INetworkingServer
    {
        /// <summary>
        /// Starts the network server on the specified address and port.
        /// </summary>
        /// <param name="address">The address to bind the server to.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="netSecret">Secret for network security (optional).</param>
        /// <returns>
        /// An <see cref="IDisposable"/> that, when disposed, will stop the server and clean up resources.
        /// </returns>
        IDisposable StartServer(string address, int port, string netSecret = "");
    }
}