using System;
using LiteNetLib.Utils;
using Shared.Replication;
using Shared.Settings;

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

        /// <summary>
        /// The game settings.
        /// </summary>
        public SettingsMessage Settings { get; set; } = new();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PeerId);
            writer.Put(ConnectionTime.ToBinary());
            writer.Put(ServerVersion);

            // Snapshot as length-prefixed payload
            var snapshotWriter = new NetDataWriter();
            InitialWorldSnapshot?.Serialize(snapshotWriter);
            writer.PutBytesWithLength(snapshotWriter.CopyData());

            // Settings as length-prefixed payload
            var settingsWriter = new NetDataWriter();
            Settings.Serialize(settingsWriter);
            writer.PutBytesWithLength(settingsWriter.CopyData());
        }

        public void Deserialize(NetDataReader reader)
        {
            PeerId = reader.GetInt();
            ConnectionTime = DateTime.FromBinary(reader.GetLong());
            ServerVersion = reader.GetString();

            // Snapshot length-prefixed payload
            var snapshotBytes = reader.GetBytesWithLength();
            if (InitialWorldSnapshot != null)
            {
                var snapshotReader = new NetDataReader(snapshotBytes);
                InitialWorldSnapshot.Deserialize(snapshotReader);
            }

            // Settings length-prefixed payload
            var settingsBytes = reader.GetBytesWithLength();
            var settingsReader = new NetDataReader(settingsBytes);
            Settings.Deserialize(settingsReader);
        }
    }
}