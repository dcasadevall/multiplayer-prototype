using System;
using System.Numerics;

namespace Shared
{
    public static class GameplayConstants
    {
        // Player constants
        public const int MaxPlayerHealth = 100;
        public const float PlayerSpeed = 5.0f;
        public const string PlayerPrefabName = "Player";
        public static readonly TimeSpan PlayerRespawnTime = TimeSpan.FromSeconds(4);

        // Player collider constants
        public static readonly Vector3 PlayerColliderBoxCenter = Vector3.Zero;
        public static readonly Vector3 PlayerColliderBoxSize = new(1, 2, 1);

        // Projectile constants
        public const float ProjectileSpeed = 8f;
        public const int ProjectileDamage = 25;
        public const uint MaxShotTickDeviation = 10; // Allow up to 10 ticks of deviation for shot validation
        public const float ProjectileSpawnHeight = 2.0f;
        public const float ProjectileSpawnForward = 0.5f;
        public const string ProjectilePrefabName = "LaserShot";
        public static readonly TimeSpan ProjectileTtl = TimeSpan.FromSeconds(4);
        public static readonly TimeSpan PlayerShotCooldown = TimeSpan.FromSeconds(0.5);

        // Projectile collider constants
        public static readonly Vector3 ProjectileColliderBoxCenter = new(0f, 0f, 0.25f);
        public static readonly Vector3 ProjectileColliderBoxSize = new(0.3f, 0.3f, 0.9f);
    }
}