using System.Drawing;
using System.Numerics;
using Shared.Damage;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Physics;
using Shared.Prediction;

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
        public static Entity CreateFromEntity(
            EntityRegistry registry,
            Entity shootingPlayerEntity,
            uint spawnTick)
        {
            var playerPosition = shootingPlayerEntity.GetRequired<PositionComponent>().Value;
            var playerRotation = shootingPlayerEntity.GetRequired<RotationComponent>().Value;
            var entityName = shootingPlayerEntity.Get<NameComponent>()?.Name ?? "Unknown";
            var entityColor = shootingPlayerEntity.Get<ColorComponent>()?.Value ?? Color.White;

            // Transform the spawn offsets by the player's rotation to get the correct world-space position
            var spawnOffset = new Vector3(0, GameplayConstants.ProjectileSpawnHeight, GameplayConstants.ProjectileSpawnForward);
            var rotatedOffset = Vector3.Transform(spawnOffset, playerRotation);
            var spawnPosition = playerPosition + rotatedOffset;

            var velocity = Vector3.Transform(Vector3.UnitZ, playerRotation) * GameplayConstants.ProjectileSpeed;

            return Create(
                registry,
                spawnPosition,
                playerRotation,
                velocity,
                spawnTick,
                entityName,
                shootingPlayerEntity.Id,
                entityColor
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
            string spawnedByName,
            EntityId sourceEntityId,
            Color color)
        {
            var projectile = registry.CreateEntity();

            // We know that position and rotation will not change after the initial spawn,
            // We can derive the position on all clients so we only send the initial tick,
            projectile.AddComponent(new VelocityComponent { Value = velocity });
            projectile.AddComponent(new RotationComponent { Value = spawnRotation });
            projectile.AddComponent(new ColorComponent { Value = color });
            projectile.AddComponent(new PredictedComponent<PositionComponent>
            {
                Mode = ReplicationMode.InitialValue,
                ServerValue = new PositionComponent { Value = spawnPosition }
            });


            // Gameplay/state components
            projectile.AddComponent<ProjectileTagComponent>();
            projectile.AddComponent(new DamageApplyingComponent
            {
                Damage = GameplayConstants.ProjectileDamage,
                SourceEntityId = sourceEntityId.Value
            });

            projectile.AddComponent(SelfDestroyingComponent.CreateWithTTL(spawnTick, GameplayConstants.ProjectileTtl.ToNumTicks()));
            projectile.AddComponent(new PrefabComponent { PrefabName = GameplayConstants.ProjectilePrefabName });
            projectile.AddComponent(new NameComponent { Name = $"Laser_{spawnedByName}" });
            projectile.AddComponent(new LocalBoundsComponent
            {
                Center = GameplayConstants.ProjectileLocalBoundsCenter,
                Size = GameplayConstants.ProjectileLocalBoundsSize
            });
            projectile.AddComponent<CollidingTagComponent>();

            return projectile;
        }
    }
}