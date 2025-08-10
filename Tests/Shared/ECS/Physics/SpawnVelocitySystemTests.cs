using System.Numerics;
using Shared;
using Shared.ECS.Entities;
using Shared.Physics;
using Shared.Prediction;
using Xunit;

namespace SharedUnitTests.ECS.Physics
{
    public class SpawnVelocitySystemTests
    {
        [Fact]
        public void Update_WhenElapsedTimeZero_KeepsSpawnPosition()
        {
            var registry = new EntityRegistry();
            var e = registry.CreateEntity();
            e.AddComponent(new DerivedPositionComponent { Position = Vector3.Zero });
            e.AddComponent(new SpawnVelocityComponent
            {
                SpawnPosition = new Vector3(1, 2, 3),
                SpawnTick = 10u,
                Velocity = new Vector3(4, 5, 6)
            });

            var system = new SpawnVelocitySystem();
            system.Update(registry, 10u, (float)SharedConstants.FixedDeltaTime.TotalSeconds);

            Assert.Equal(new Vector3(1, 2, 3), e.GetRequired<DerivedPositionComponent>().Position);
        }

        [Fact]
        public void Update_WhenElapsedTimeAdvances_ComputesPositionFromSpawn()
        {
            var registry = new EntityRegistry();
            var e = registry.CreateEntity();
            e.AddComponent(new DerivedPositionComponent { Position = Vector3.Zero });
            e.AddComponent(new SpawnVelocityComponent
            {
                SpawnPosition = new Vector3(0, 0, 0),
                SpawnTick = 0u,
                Velocity = new Vector3(10, 0, 0)
            });

            var system = new SpawnVelocitySystem();
            // After 3 ticks
            system.Update(registry, 3u, (float)SharedConstants.FixedDeltaTime.TotalSeconds);

            var expected = new Vector3(10, 0, 0) * (3f * (float)SharedConstants.FixedDeltaTime.TotalSeconds);
            var actual = e.GetRequired<DerivedPositionComponent>().Position;
            Assert.InRange(actual.X, expected.X - 1e-5f, expected.X + 1e-5f);
            Assert.InRange(actual.Y, expected.Y - 1e-5f, expected.Y + 1e-5f);
            Assert.InRange(actual.Z, expected.Z - 1e-5f, expected.Z + 1e-5f);
        }
    }
}