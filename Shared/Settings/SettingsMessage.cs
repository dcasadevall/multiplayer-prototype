using System.Text.Json;
using LiteNetLib.Utils;

namespace Shared.Settings
{
    /// <summary>
    /// A message containing all game settings that are sent from the server to the client on connection.
    /// </summary>
    public class SettingsMessage : INetSerializable
    {
        public PlayerSettings Player { get; set; } = new();
        public ProjectileSettings Projectile { get; set; } = new();
        public BotSettings Bot { get; set; } = new();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(JsonSerializer.Serialize(Player));
            writer.Put(JsonSerializer.Serialize(Projectile));
            writer.Put(JsonSerializer.Serialize(Bot));
        }

        public void Deserialize(NetDataReader reader)
        {
            Player = JsonSerializer.Deserialize<PlayerSettings>(reader.GetString())!;
            Projectile = JsonSerializer.Deserialize<ProjectileSettings>(reader.GetString())!;
            Bot = JsonSerializer.Deserialize<BotSettings>(reader.GetString())!;
        }
    }
}