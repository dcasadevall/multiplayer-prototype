using Shared.ECS;

namespace Server.AI
{
    /// <summary>
    /// Component used to track the cooldown for shooting actions.
    /// </summary>
    public class ShootingCooldownComponent : IServerComponent
    {
        /// <summary>
        /// When the cooldown ends, represented as a tick count.
        /// </summary>
        public uint EndTick { get; set; }
    }
}