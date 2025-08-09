using Shared.ECS.Entities;

namespace Shared.ECS
{
    /// <summary>
    /// Interface for all ECS systems. Systems encapsulate logic that operates on entities
    /// with specific component sets, and are invoked by the world on each eligible tick.
    /// 
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    ///   <item>Implement <see cref="Update"/> to process entities each tick.</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// The ECS world calls <see cref="Update"/> for each system at its configured tick interval,
    /// passing the <see cref="EntityRegistry"/> and the elapsed time since the last update.
    /// </para>
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Called by the world at each system tick.
        /// Implement logic to process relevant entities here.
        /// </summary>
        /// <param name="registry">The entity manager for querying and manipulating entities.</param>
        /// <param name="tickNumber">The current tick number in the simulation, starting from 1.</param>
        /// <param name="deltaTime">The time in seconds since the last update for this system.</param>
        void Update(EntityRegistry registry, uint tickNumber, float deltaTime);
    }
}