using System.Numerics;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.Input;

namespace Shared.ECS.Archetypes
{
    /// <summary>
    /// Defines the complete set of components that a Projectile entity should have.
    /// </summary>
    public static class ProjectileArchetype
    {
        /// <summary>
        /// Creates a new projectile entity with all required components.
        /// </summary>
        public static Entity Create(
            EntityRegistry registry,
            Vector3 spawnPosition,
            Vector3 velocity,
            uint serverTick,
            int spawnedByPeerId,
            System.Guid predictedLocalEntityId)
        {
            var projectile = registry.CreateEntity();

            // Predicted spatial components
            projectile.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            projectile.AddPredictedComponent(new VelocityComponent { Value = velocity });

            // Gameplay/state components
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new DamageApplyingComponent { Damage = GameplayConstants.ProjectileDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(serverTick, GameplayConstants.ProjectileTtlTicks));
            projectile.AddComponent(new SpawnAuthorityComponent
            {
                SpawnedByPeerId = spawnedByPeerId,
                LocalEntityId = predictedLocalEntityId,
                SpawnTick = serverTick
            });

            // Network replication
            projectile.AddComponent(new ReplicatedTagComponent());

            return projectile;
        }
    }
}