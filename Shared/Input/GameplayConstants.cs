namespace Shared.Input
{
    public static class GameplayConstants
    {
        // Player movement constants
        public const float PlayerSpeed = 5.0f;

        // Laser/Projectile constants
        public const uint PlayerShotCooldownTicks = 15; // 0.5 seconds at 30 ticks/sec
        public const float ProjectileSpeed = 15f;
        public const uint ProjectileTtlTicks = 120; // 4 seconds at 30 ticks/sec
        public const int ProjectileDamage = 25;
        public const uint MaxShotTickDeviation = 10; // Allow up to 10 ticks of deviation for shot validation
        public const string ProjectilePrefab = "Projectile";
    }
}