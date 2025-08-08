using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Health;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.Health
{
    public class DamageSystemTests
    {
        [Fact]
        public void Update_AppliesDamageAndDestroysProjectile()
        {
            // Arrange
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var system = new DamageSystem(collisionDetector);

            // Create target entity with health
            var target = registry.CreateEntity();
            target.AddComponent(new HealthComponent(100));

            // Create projectile entity
            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25, CanDamageSelf = false });
            projectile.AddComponent(new SpawnAuthorityComponent { SpawnedByPeerId = 1 });

            // Simulate collision
            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            // Act
            system.Update(registry, 1, 0.016f);

            // Assert
            Assert.Equal(75, target.GetRequired<HealthComponent>().CurrentHealth);
            Assert.False(registry.TryGet(projectile.Id, out _)); // projectile destroyed
        }

        [Fact]
        public void Update_DoesNotApplyDamage_WhenNoHealthComponent()
        {
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var system = new DamageSystem(collisionDetector);

            var target = registry.CreateEntity(); // No health component

            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25 });
            projectile.AddComponent(new SpawnAuthorityComponent { SpawnedByPeerId = 1 });

            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            system.Update(registry, 1, 0.016f);

            Assert.True(registry.TryGet(target.Id, out _)); // target not destroyed
        }

        [Fact]
        public void Update_PreventsFriendlyFire_WhenCanDamageSelfIsFalse()
        {
            var registry = new EntityRegistry();
            var collisionDetector = Substitute.For<ICollisionDetector>();
            var system = new DamageSystem(collisionDetector);

            var target = registry.CreateEntity();
            target.AddComponent(new HealthComponent(100));
            target.AddComponent(new PeerComponent { PeerId = 1 });

            var projectile = registry.CreateEntity();
            projectile.AddComponent(new DamageApplyingComponent { Damage = 25, CanDamageSelf = false });
            projectile.AddComponent(new SpawnAuthorityComponent { SpawnedByPeerId = 1 });

            collisionDetector.GetCollisionsFor(projectile.Id).Returns([new EntityId(target.Id.Value)]);

            system.Update(registry, 1, 0.016f);

            Assert.Equal(100, target.GetRequired<HealthComponent>().CurrentHealth); // No damage
        }
    }
}