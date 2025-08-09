using System;
using LiteNetLib.Utils;

namespace Shared.Input
{
    /// <summary>
    /// Message sent from client to server when the player shoots.
    /// Contains shooting information for server validation and projectile spawning.
    /// It is serialized using binary format for efficiency.
    /// </summary>
    public class PlayerShotMessage : INetSerializable
    {
        /// <summary>
        /// The tick at which the shot was fired on the client.
        /// </summary>
        public uint Tick { get; set; }

        /// <summary>
        /// Local entity ID of the predicted projectile on the client.
        /// Used for associating client prediction with server authority.
        /// </summary>
        public Guid PredictedProjectileId { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Tick);
            writer.Put(PredictedProjectileId.ToByteArray());
        }

        public void Deserialize(NetDataReader reader)
        {
            Tick = reader.GetUInt();
            var bytes = new byte[16];
            reader.GetBytes(bytes, 16);
            PredictedProjectileId = new Guid(bytes);
        }
    }
}