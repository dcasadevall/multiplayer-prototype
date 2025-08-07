using System.Numerics;

namespace Shared.Input
{
    public static class GameplayConstants
    {
        // Player constants
        public const float PlayerSpeed = 5.0f;
        public const string PlayerPrefabName = "Player";

        // Projectile constants
        public const uint PlayerShotCooldownTicks = 15; // 0.5 seconds at 30 ticks/sec
        public const float ProjectileSpeed = 8f;
        public const uint ProjectileTtlTicks = 120; // 4 seconds at 30 ticks/sec
        public const int ProjectileDamage = 25;
        public const uint MaxShotTickDeviation = 10; // Allow up to 10 ticks of deviation for shot validation
        public const float ProjectileSpawnHeight = 2.0f;
        public const string ProjectilePrefabName = "LaserShot";
        public static readonly Vector3 ProjectileColliderBoxCenter = new(0f, 0f, 0.25f);
        public static readonly Vector3 ProjectileColliderBoxSize = new(0.3f, 0.3f, 0.9f);
    }
}