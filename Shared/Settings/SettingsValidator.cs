using System;
using Shared.Scheduling;

namespace Shared.Settings
{
    public class SettingsValidationException : Exception
    {
        public SettingsValidationException(string message) : base(message)
        {
        }
    }

    public class SettingsValidator : IInitializable
    {
        private readonly PlayerSettings _playerSettings;
        private readonly ProjectileSettings _projectileSettings;
        private readonly BotSettings _botSettings;
        private readonly SimulationSettings _simulationSettings;

        public SettingsValidator(
            PlayerSettings playerSettings,
            ProjectileSettings projectileSettings,
            BotSettings botSettings,
            SimulationSettings simulationSettings)
        {
            _playerSettings = playerSettings ?? throw new ArgumentNullException(nameof(playerSettings));
            _projectileSettings = projectileSettings ?? throw new ArgumentNullException(nameof(projectileSettings));
            _botSettings = botSettings ?? throw new ArgumentNullException(nameof(botSettings));
            _simulationSettings = simulationSettings ?? throw new ArgumentNullException(nameof(simulationSettings));
        }

        public void Initialize()
        {
            // Player Settings
            if (_playerSettings.MaxPlayerHealth <= 0)
                throw new SettingsValidationException("Player health must be greater than 0.");
            if (_playerSettings.PlayerSpeed <= 0)
                throw new SettingsValidationException("Player speed must be greater than 0.");
            if (_playerSettings.PlayerRespawnTime <= TimeSpan.Zero)
                throw new SettingsValidationException("Player respawn time must be greater than 0.");
            if (_playerSettings.PlayerShotCooldown <= TimeSpan.Zero)
                throw new SettingsValidationException("Player shot cooldown must be greater than 0.");

            // Projectile Settings
            if (_projectileSettings.ProjectileSpeed <= 0)
                throw new SettingsValidationException("Projectile speed must be greater than 0.");
            if (_projectileSettings.ProjectileDamage <= 0)
                throw new SettingsValidationException("Projectile damage must be greater than 0.");
            if (_projectileSettings.ProjectileTtl <= TimeSpan.Zero)
                throw new SettingsValidationException("Projectile TTL must be greater than 0.");

            // Bot Settings
            if (_botSettings.MaxBotHealth <= 0)
                throw new SettingsValidationException("Bot health must be greater than 0.");
            if (_botSettings.BotAttackDistance <= 0)
                throw new SettingsValidationException("Bot attack distance must be greater than 0.");
            if (_botSettings.BotRetreatHealthPercentThreshold is <= 0 or >= 1)
                throw new SettingsValidationException("Bot retreat health threshold must be between 0 and 1.");
            if (_botSettings.BotRetreatSpeed <= 0)
                throw new SettingsValidationException("Bot retreat speed must be greater than 0.");
            if (_botSettings.BotApproachSpeed <= 0)
                throw new SettingsValidationException("Bot approach speed must be greater than 0.");
            if (_botSettings.BoatRoamRadius <= 0)
                throw new SettingsValidationException("Bot roam radius must be greater than 0.");
            if (_botSettings.BotRoamInterval <= TimeSpan.Zero)
                throw new SettingsValidationException("Bot roam interval must be greater than 0.");
            if (_botSettings.BotShootingCooldown <= TimeSpan.Zero)
                throw new SettingsValidationException("Bot shooting cooldown must be greater than 0.");
            if (_botSettings.BotRespawnTime <= TimeSpan.Zero)
                throw new SettingsValidationException("Bot respawn time must be greater than 0.");
                
            // Simulation Settings
            if (_simulationSettings.WorldTicksPerSecond <= 0)
                throw new SettingsValidationException("World ticks per second must be greater than 0.");
            if (_simulationSettings.FixedDeltaTime <= TimeSpan.Zero)
                throw new SettingsValidationException("Fixed delta time must be greater than 0.");
        }
    }
}