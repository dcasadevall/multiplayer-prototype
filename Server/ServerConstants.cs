namespace Server;

public class ServerConstants
{
    public const float BotAttackDistance = 10f;
    public const float BotRetreatHealthPercentThreshold = 0.3f;
    public const float BotRetreatSpeed = 10.0f;
    public const float BotApproachSpeed = 2.0f;
    public const float BoatRoamRadius = 5.0f;
    public static readonly TimeSpan BotRoamInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan BotShootingCooldown = TimeSpan.FromSeconds(1);
}