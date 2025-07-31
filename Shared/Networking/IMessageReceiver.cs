using System;

namespace Shared.Networking;

/// <summary>
/// Defines an abstraction for receiving messages from network peers.
/// <para>
/// Implementations of <see cref="IMessageReceiver"/> are responsible for receiving messages from peers,
/// parsing message types, and notifying subscribers of incoming messages. This abstraction allows
/// the networking layer to be decoupled from the underlying transport implementation.
/// </para>
/// </summary>
public interface IMessageReceiver
{
    /// <summary>
    /// Event fired when a message is received from a peer.
    /// </summary>
    event Action<MessageType, byte[]> OnMessageReceived;

    /// <summary>
    /// Starts listening for incoming messages.
    /// </summary>
    void StartListening();

    /// <summary>
    /// Stops listening for incoming messages.
    /// </summary>
    void StopListening();
} 