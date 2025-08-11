using System;
using System.Numerics;

namespace Shared.Settings
{
    public class SimulationSettings
    {
        /// <summary>
        /// Tick rate for the world simulation.
        /// This defines how often the world updates its state.
        /// </summary>
        public uint WorldTicksPerSecond { get; } = 30;

        /// <summary>
        /// The fixed delta time for the world simulation.
        /// </summary>
        public TimeSpan FixedDeltaTime { get; } = TimeSpan.FromSeconds(1.0f / 30.0f);
    }

    public class BotSettings
    {
        /// <summary>
        /// The maximum health of a bot.
        /// </summary>
        public int MaxBotHealth { get; set; } = 100;

        /// <summary>
        /// The distance at which a bot will start attacking a player.
        /// </summary>
        public float BotAttackDistance { get; set; } = 10f;

        /// <summary>
        /// The health percentage at which a bot will retreat.
        /// </summary>
        public float BotRetreatHealthPercentThreshold { get; set; } = 0.3f;

        /// <summary>
        /// The speed at which a bot will retreat.
        /// </summary>
        public float BotRetreatSpeed { get; set; } = 10.0f;

        /// <summary>
        /// The speed at which a bot will approach a player.
        /// </summary>
        public float BotApproachSpeed { get; set; } = 2.0f;

        /// <summary>
        /// The radius in which a bot will roam.
        /// </summary>
        public float BoatRoamRadius { get; set; } = 5.0f;

        /// <summary>
        /// The interval at which a bot will roam.
        /// </summary>
        public TimeSpan BotRoamInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The cooldown between bot shots.
        /// </summary>
        public TimeSpan BotShootingCooldown { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The time it takes for a bot to respawn.
        /// </summary>
        public TimeSpan BotRespawnTime { get; set; } = TimeSpan.FromSeconds(3);

        // We reuse the player prefab for bots, can change this if needed
        /// <summary>
        /// The name of the prefab to use for bots.
        /// </summary>
        public string PrefabName { get; set; } = "Player";

        /// <summary>
        /// The center of the bot's local bounds.
        /// </summary>
        public Vector3 LocalBoundsCenter { get; set; } = Vector3.UnitY;

        /// <summary>
        /// The size of the bot's local bounds.
        /// </summary>
        public Vector3 LocalBoundsSize { get; set; } = new(1, 2, 1);
    }

    public class PlayerSettings
    {
        /// <summary>
        /// The maximum health of a player.
        /// </summary>
        public int MaxPlayerHealth { get; set; } = 100;

        /// <summary>
        /// The movement speed of a player.
        /// </summary>
        public float PlayerSpeed { get; set; } = 5.0f;

        /// <summary>
        /// The name of the prefab to use for players.
        /// </summary>
        public string PlayerPrefabName { get; set; } = "Player";

        /// <summary>
        /// The time it takes for a player to respawn.
        /// </summary>
        public TimeSpan PlayerRespawnTime { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// The center of the player's local bounds.
        /// </summary>
        public Vector3 PlayerLocalBoundsCenter { get; set; } = Vector3.UnitY;

        /// <summary>
        /// The size of the player's local bounds.
        /// </summary>
        public Vector3 PlayerLocalBoundsSize { get; set; } = new(1, 2, 1);

        /// <summary>
        /// The cooldown between player shots.
        /// </summary>
        public TimeSpan PlayerShotCooldown { get; set; } = TimeSpan.FromSeconds(0.5);
    }

    public class ProjectileSettings
    {
        /// <summary>
        /// The speed of a projectile.
        /// </summary>
        public float ProjectileSpeed { get; set; } = 8f;

        /// <summary>
        /// The damage a projectile deals.
        /// </summary>
        public int ProjectileDamage { get; set; } = 25;

        /// <summary>
        /// The maximum tick deviation for a shot to be considered valid.
        /// </summary>
        public uint MaxShotTickDeviation { get; set; } = 10;

        /// <summary>
        /// The height at which a projectile is spawned.
        /// </summary>
        public float ProjectileSpawnHeight { get; set; } = 1.0f;

        /// <summary>
        /// The forward offset at which a projectile is spawned.
        /// </summary>
        public float ProjectileSpawnForward { get; set; } = 0.5f;

        /// <summary>
        /// The name of the prefab to use for projectiles.
        /// </summary>
        public string ProjectilePrefabName { get; set; } = "LaserShot";

        /// <summary>
        /// The time to live of a projectile.
        /// </summary>
        public TimeSpan ProjectileTtl { get; set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// The center of the projectile's local bounds.
        /// </summary>
        public Vector3 ProjectileLocalBoundsCenter { get; set; } = new(0f, 0f, 0.25f);

        /// <summary>
        /// The size of the projectile's local bounds.
        /// </summary>
        public Vector3 ProjectileLocalBoundsSize { get; set; } = new(0.3f, 0.3f, 0.4f);
    }
}