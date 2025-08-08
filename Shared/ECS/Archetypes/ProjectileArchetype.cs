using System.Numerics;
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
        /// A helper method that makes more assumptions about the context in which the projectile is created.
        /// It is intended to be used when the projectile is created from a player action, such as shooting.
        /// It is tuned specifically for this example, and may not be suitable for other games
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="shootingPlayerEntity"></param>
        /// <param name="spawnTick"></param>
        /// <returns></returns>
        public static Entity CreateFromPlayer(
            EntityRegistry registry,
            Entity shootingPlayerEntity,
            uint spawnTick)
        {
            var playerPosition = shootingPlayerEntity.GetRequired<PositionComponent>().Value;
            var peerId = shootingPlayerEntity.GetRequired<PeerComponent>().PeerId;
            var spawnPosition = playerPosition +
                                Vector3.UnitY * GameplayConstants.ProjectileSpawnHeight +
                                Vector3.UnitZ * GameplayConstants.ProjectileSpawnForward;

            // Position and velocity
            var playerRotation = shootingPlayerEntity.GetRequired<RotationComponent>().Value;
            var velocity = Vector3.Transform(new Vector3(0, 0, 1), playerRotation) * GameplayConstants.ProjectileSpeed;
            var spawnRotation = playerRotation;

            return Create(
                registry,
                spawnPosition,
                spawnRotation,
                velocity,
                spawnTick,
                peerId
            );
        }

        /// <summary>
        /// Creates a new projectile entity with all required components.
        /// </summary>
        public static Entity Create(
            EntityRegistry registry,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            Vector3 velocity,
            uint spawnTick,
            int spawnedByPeerId)
        {
            var projectile = registry.CreateEntity();

            // Predicted spatial components
            projectile.AddPredictedComponent(new RotationComponent { Value = spawnRotation });
            projectile.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            projectile.AddPredictedComponent(new VelocityComponent { Value = velocity });

            // Gameplay/state components
            projectile.AddComponent(new ProjectileTagComponent());
            projectile.AddComponent(new DamageApplyingComponent { Damage = GameplayConstants.ProjectileDamage });
            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(spawnTick, GameplayConstants.ProjectileTtlTicks));
            projectile.AddComponent(new PrefabComponent { PrefabName = GameplayConstants.ProjectilePrefabName });
            projectile.AddComponent(new NameComponent { Name = $"Laser_{spawnedByPeerId}" });

            // Network replication
            projectile.AddComponent(new ReplicatedTagComponent());

            return projectile;
        }
    }
}