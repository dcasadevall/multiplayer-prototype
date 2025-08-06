using System.Text.Json.Serialization;

namespace Shared.ECS.Components
{
    /// <summary>
    /// Component that causes an entity to be automatically destroyed when a specific tick is reached.
    /// Used for temporary entities like projectiles, explosions, or timed effects.
    /// </summary>
    public class SelfDestroyingComponent : IComponent
    {
        /// <summary>
        /// The tick at which this entity should be destroyed.
        /// When the world tick reaches this value, the entity will be removed.
        /// </summary>
        [JsonPropertyName("destroyAtTick")]
        public uint DestroyAtTick { get; set; }

        /// <summary>
        /// Whether this entity has been marked for destruction.
        /// Used to prevent multiple destruction attempts.
        /// </summary>
        [JsonIgnore]
        public bool IsMarkedForDestruction { get; set; } = false;

        public SelfDestroyingComponent() { }

        public SelfDestroyingComponent(uint destroyAtTick)
        {
            DestroyAtTick = destroyAtTick;
        }

        /// <summary>
        /// Creates a self-destroying component that will destroy the entity after the specified number of ticks.
        /// </summary>
        /// <param name="currentTick">The current tick</param>
        /// <param name="ticksToLive">How many ticks the entity should exist</param>
        /// <returns>A configured SelfDestroyingComponent</returns>
        public static SelfDestroyingComponent CreateWithTTL(uint currentTick, uint ticksToLive)
        {
            return new SelfDestroyingComponent(currentTick + ticksToLive);
        }
    }
}