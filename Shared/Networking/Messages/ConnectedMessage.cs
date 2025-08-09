using System;
using LiteNetLib.Utils;
using Shared.ECS.Replication;

namespace Shared.Networking.Messages
{
    /// <summary>
    /// Message sent by the server immediately when a client connects.
    /// Contains the assigned peer ID that the client must use for all subsequent communication.
    /// This is the first message in the handshake process.
    /// </summary>
    public class ConnectedMessage : INetSerializable
    {
        /// <summary>
        /// The peer ID assigned to the client by the server.
        /// This ID must be used for all subsequent client-to-server messages.
        /// </summary>
        public int PeerId { get; set; }

        /// <summary>
        /// Timestamp when the connection was established (server time).
        /// </summary>
        public DateTime ConnectionTime { get; set; }

        /// <summary>
        /// Server version information for compatibility checking.
        /// </summary>
        public string ServerVersion { get; set; } = "1.0.0";

        /// <summary>
        /// The initial state of the world when the client connects.
        /// </summary>
        public WorldDeltaMessage? InitialWorldSnapshot { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PeerId);
            writer.Put(ConnectionTime.ToBinary());
            writer.Put(ServerVersion);
            InitialWorldSnapshot?.Serialize(writer);
        }

        public void Deserialize(NetDataReader reader)
        {
            PeerId = reader.GetInt();
            ConnectionTime = DateTime.FromBinary(reader.GetLong());
            ServerVersion = reader.GetString();
            InitialWorldSnapshot?.Deserialize(reader);
        }
    }
}