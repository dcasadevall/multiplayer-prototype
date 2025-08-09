using NSubstitute;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.Damage
{
    /// <summary>
    /// Unit tests for <see cref="DamageSystem"/>.
    /// 
    /// Test structure follows the Arrange-Act-Assert pattern for clarity and maintainability.
    /// See: https://medium.com/@kaanfurkanc/unit-testing-best-practices-3a8b0ddd88b5
    /// </summary>
    public class DamageSystemTests
    {
        [Fact]
        public void Update_AppliesDamageAndDestroysProjectile()
        {
            // Arrange: Setup registry, system, and entities
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var logger = Substitute.For<ILogger>();
            var system = new DamageSystem(collisionDetector, logger);

            // Create target entity with health
            var target = registry.CreateEntity();
            target.AddComponent(new HealthComponent(100));

            // Create projectile entity
            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25, CanDamageSelf = false });
            projectile.AddComponent(new SpawnAuthorityComponent { SpawnedByPeerId = 1 });

            // Simulate collision
            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            // Act: Run the damage system update
            system.Update(registry, 1, 0.016f);

            // Assert: Target health reduced, projectile destroyed, target still exists
            Assert.Equal(75, target.GetRequired<HealthComponent>().CurrentHealth);
            Assert.False(registry.TryGet(projectile.Id, out _)); // projectile destroyed
            Assert.True(registry.TryGet(target.Id, out _)); // Target still exists
        }

        [Fact]
        public void Update_DoesNotApplyDamage_WhenNoHealthComponent()
        {
            // Arrange: Setup registry, system, and entities
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var logger = Substitute.For<ILogger>();
            var system = new DamageSystem(collisionDetector, logger);

            var target = registry.CreateEntity(); // No health component

            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25 });
            projectile.AddComponent(new SpawnAuthorityComponent { SpawnedByPeerId = 1 });

            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            // Act: Run the damage system update
            system.Update(registry, 1, 0.016f);

            // Assert: Target not destroyed
            Assert.True(registry.TryGet(target.Id, out _)); // target not destroyed
        }

        [Fact]
        public void Update_PreventsFriendlyFire_WhenCanDamageSelfIsFalse()
        {
            // Arrange: Setup registry, system, and entities
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var logger = Substitute.For<ILogger>();
            var system = new DamageSystem(collisionDetector, logger);

            var target = registry.CreateEntity();
            target.AddComponent(new HealthComponent(100));
            target.AddComponent(new PeerComponent { PeerId = 1 });

            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25, CanDamageSelf = false, SourceEntityId = target.Id.Value });

            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            // Act: Run the damage system update
            system.Update(registry, 1, 0.016f);

            // Assert: No damage applied due to friendly fire prevention
            Assert.Equal(100, target.GetRequired<HealthComponent>().CurrentHealth); // No damage
        }
    }
}