using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Physics;

namespace Shared.Health
{
    /// <summary>
    /// System that applies damage to entities based on collisions.
    /// This system runs on both server and client.
    /// It checks for collisions, applies damage, and destroys the damaging entity after impact.
    /// </summary>
    public class DamageSystem : ISystem
    {
        private readonly ICollisionDetector _collisionDetector;

        /// <summary>
        /// Constructs a DamageSystem with the given collision detector.
        /// </summary>
        /// <param name="collisionDetector">Collision detector used to find impacted entities.</param>
        public DamageSystem(ICollisionDetector collisionDetector)
        {
            _collisionDetector = collisionDetector;
        }

        /// <summary>
        /// Processes all projectiles, applies damage to collided entities, and destroys projectiles.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">Current simulation tick.</param>
        /// <param name="deltaTime">Delta time for the tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var projectiles = registry.WithAll<DamageApplyingComponent, SpawnAuthorityComponent>().ToList();

            foreach (var projectile in projectiles)
            {
                var collisions = _collisionDetector.GetCollisionsFor(projectile.Id);
                if (collisions.Count == 0)
                    continue;

                var damageComponent = projectile.GetRequired<DamageApplyingComponent>();
                var projectileOwner = projectile.Get<SpawnAuthorityComponent>();

                foreach (var collisionId in collisions)
                {
                    if (!registry.TryGet(collisionId, out var targetEntity))
                        continue;

                    if (!targetEntity.Has<HealthComponent>())
                        continue;

                    // Prevent friendly fire if not allowed
                    if (projectileOwner != null && targetEntity.Has<PeerComponent>() &&
                        targetEntity.GetRequired<PeerComponent>().PeerId == projectileOwner.SpawnedByPeerId &&
                        !damageComponent.CanDamageSelf)
                    {
                        continue;
                    }

                    var healthComponent = targetEntity.GetRequired<HealthComponent>();
                    healthComponent.CurrentHealth -= damageComponent.Damage;
                }

                registry.DestroyEntity(projectile.Id);
            }
        }
    }
}