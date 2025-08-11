using System.Text.Json;
using LiteNetLib.Utils;

namespace Shared.Settings
{
    /// <summary>
    /// A message containing all game settings that are sent from the server to the client on connection.
    /// </summary>
    public class SettingsMessage : INetSerializable
    {
        public PlayerSettings PlayerSettings { get; set; } = new();
        public ProjectileSettings ProjectileSettings { get; set; } = new();
        public BotSettings BotSettings { get; set; } = new();
        public SimulationSettings SimulationSettings { get; set; } = new();

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(JsonSerializer.Serialize(PlayerSettings));
            writer.Put(JsonSerializer.Serialize(ProjectileSettings));
            writer.Put(JsonSerializer.Serialize(BotSettings));
            writer.Put(JsonSerializer.Serialize(SimulationSettings));
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerSettings = JsonSerializer.Deserialize<PlayerSettings>(reader.GetString())!;
            ProjectileSettings = JsonSerializer.Deserialize<ProjectileSettings>(reader.GetString())!;
            BotSettings = JsonSerializer.Deserialize<BotSettings>(reader.GetString())!;
            SimulationSettings = JsonSerializer.Deserialize<SimulationSettings>(reader.GetString())!;
        }
    }
}