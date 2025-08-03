using Shared.ECS;
using UnityEngine;

namespace Core.ECS.Rendering
{
    /// <summary>
    /// EntityViewRegistry interface for managing entity views in Unity.
    /// Entity views are Unity GameObjects that represent ECS entities visually.
    /// They can be used to render entities in the game world, allowing for
    /// visual representation of entities based on their components.
    /// </summary>
    public interface IEntityViewRegistry
    {
        /// <summary>
        /// Tries to get the Unity GameObject for a given entity ID.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="view">The Transform of the Unity GameObject representing the entity.</param>
        public bool TryGetEntityView(EntityId entityId, out Transform view);
        /// <summary>
        /// Gets the Unity GameObject for a given entity ID.
        /// Returns null if the entity does not have a view.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <returns>Transform of the Unity GameObject representing the entity.</returns>
        public Transform GetEntityView(EntityId entityId);
    }
}