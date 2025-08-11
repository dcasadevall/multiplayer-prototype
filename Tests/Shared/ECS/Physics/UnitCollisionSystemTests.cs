using System.Numerics;
using Shared.ECS.Entities;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Physics
{
    public class UnitCollisionSystemTests
    {
        [Fact]
        public void Update_WhenTwoUnitsOverlap_SeparatesThemSoTheyNoLongerCollide()
        {
            var registry = new EntityRegistry();

            var a = registry.CreateEntity();
            a.AddComponent(new PositionComponent { Value = new Vector3(0f, 0f, 0f) });
            a.AddComponent(new RotationComponent { Value = Quaternion.Identity });
            a.AddComponent(new LocalBoundsComponent { Center = Vector3.Zero, Size = new Vector3(1f, 2f, 1f) });
            a.AddComponent<CollidingTagComponent>();

            var b = registry.CreateEntity();
            b.AddComponent(new PositionComponent { Value = new Vector3(0.4f, 0f, 0f) });
            b.AddComponent(new RotationComponent { Value = Quaternion.Identity });
            b.AddComponent(new LocalBoundsComponent { Center = Vector3.Zero, Size = new Vector3(1f, 2f, 1f) });
            b.AddComponent<CollidingTagComponent>();

            // Update AABBs and detect collisions
            var aabbSystem = new WorldAABBUpdateSystem();
            aabbSystem.Update(registry, 0u, 0f);

            var collisionDetector = new CollisionSystem();
            collisionDetector.Update(registry, 0u, 0f);

            Assert.True(collisionDetector.AreColliding(a.Id, b.Id));

            // Resolve overlaps
            var unitCollision = new UnitCollisionSystem(collisionDetector);
            unitCollision.Update(registry, 0u, 0f);

            // Recompute AABBs and collisions after resolution
            aabbSystem.Update(registry, 1u, 0f);
            collisionDetector.Update(registry, 1u, 0f);

            Assert.False(collisionDetector.AreColliding(a.Id, b.Id));
        }

        [Fact]
        public void Update_WhenEitherEntityDoesNotOccupySpace_DoesNotMoveEitherEntity()
        {
            var registry = new EntityRegistry();

            var a = registry.CreateEntity();
            a.AddComponent(new PositionComponent { Value = new Vector3(0f, 0f, 0f) });
            a.AddComponent(new RotationComponent { Value = Quaternion.Identity });
            a.AddComponent(new LocalBoundsComponent { Center = Vector3.Zero, Size = new Vector3(1f, 2f, 1f) });
            a.AddComponent<CollidingTagComponent>();
            a.AddComponent<DoesNotOccupySpaceTagComponent>();

            var b = registry.CreateEntity();
            b.AddComponent(new PositionComponent { Value = new Vector3(0.4f, 0f, 0f) });
            b.AddComponent(new RotationComponent { Value = Quaternion.Identity });
            b.AddComponent(new LocalBoundsComponent { Center = Vector3.Zero, Size = new Vector3(1f, 2f, 1f) });
            b.AddComponent<CollidingTagComponent>();

            var posABefore = a.GetRequired<PositionComponent>().Value;
            var posBBefore = b.GetRequired<PositionComponent>().Value;

            var aabbSystem = new WorldAABBUpdateSystem();
            aabbSystem.Update(registry, 0u, 0f);

            var collisionDetector = new CollisionSystem();
            collisionDetector.Update(registry, 0u, 0f);

            Assert.True(collisionDetector.AreColliding(a.Id, b.Id));

            var unitCollision = new UnitCollisionSystem(collisionDetector);
            unitCollision.Update(registry, 0u, 0f);

            // Positions should remain unchanged because separation is skipped
            var posAAfter = a.GetRequired<PositionComponent>().Value;
            var posBAfter = b.GetRequired<PositionComponent>().Value;

            Assert.Equal(posABefore, posAAfter);
            Assert.Equal(posBBefore, posBAfter);
        }
    }
}
