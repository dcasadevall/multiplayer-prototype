using System.Numerics;
using LiteNetLib.Utils;

namespace Shared.Input
{
    /// <summary>
    /// Represents a player movement input message sent from the client to the server.
    /// This message is used for networked movement prediction and reconciliation.
    /// It is serialized using binary format for efficiency.
    /// </summary>
    public class PlayerMovementMessage : INetSerializable
    {
        /// <summary>
        /// The X component of the player's movement input (e.g., WASD or joystick direction).
        /// </summary>
        public float MoveDirectionX { get; set; }

        /// <summary>
        /// The Y component of the player's movement input (e.g., WASD or joystick direction).
        /// </summary>
        public float MoveDirectionY { get; set; }

        /// <summary>
        /// The tick count of the client when this input was generated.
        /// </summary>
        public long ClientTick { get; set; } = 0;

        /// <summary>
        /// Gets or sets the movement direction as a <see cref="Vector2"/>.
        /// This property is not serialized; it is provided for convenience in code.
        /// </summary>
        public Vector2 MoveDirection
        {
            get => new(MoveDirectionX, MoveDirectionY);
            set
            {
                MoveDirectionX = value.X;
                MoveDirectionY = value.Y;
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(MoveDirectionX);
            writer.Put(MoveDirectionY);
            writer.Put(ClientTick);
        }

        public void Deserialize(NetDataReader reader)
        {
            MoveDirectionX = reader.GetFloat();
            MoveDirectionY = reader.GetFloat();
            ClientTick = reader.GetLong();
        }
    }
}