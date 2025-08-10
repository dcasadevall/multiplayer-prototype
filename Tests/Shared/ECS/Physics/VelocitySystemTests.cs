using System.Numerics;
using Shared;
using Shared.ECS.Entities;
using Shared.Physics;
using Xunit;

namespace SharedUnitTests.ECS.Physics
{
    public class VelocitySystemTests
    {
        [Fact]
        public void Update_WhenEntityHasPositionAndVelocity_AdvancesPositionByVelocityTimesFixedDt()
        {
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent { Value = new Vector3(1f, 2f, 3f) });
            entity.AddComponent(new VelocityComponent { Value = new Vector3(4f, 0f, -2f) });

            var system = new VelocitySystem();
            system.Update(registry, 0u, (float)SharedConstants.FixedDeltaTime.TotalSeconds);

            var expected = new Vector3(1f, 2f, 3f) + new Vector3(4f, 0f, -2f) * (float)SharedConstants.FixedDeltaTime.TotalSeconds;
            var actual = entity.GetRequired<PositionComponent>().Value;

            Assert.InRange(actual.X, expected.X - 1e-6f, expected.X + 1e-6f);
            Assert.InRange(actual.Y, expected.Y - 1e-6f, expected.Y + 1e-6f);
            Assert.InRange(actual.Z, expected.Z - 1e-6f, expected.Z + 1e-6f);
        }

        [Fact]
        public void Update_WhenMultipleTicks_AccumulatesPosition()
        {
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new PositionComponent { Value = new Vector3(0f, 0f, 0f) });
            entity.AddComponent(new VelocityComponent { Value = new Vector3(1f, 2f, 3f) });

            var system = new VelocitySystem();

            for (int i = 0; i < 5; i++)
            {
                system.Update(registry, (uint)i, (float)SharedConstants.FixedDeltaTime.TotalSeconds);
            }

            var expected = new Vector3(1f, 2f, 3f) * (5f * (float)SharedConstants.FixedDeltaTime.TotalSeconds);
            var actual = entity.GetRequired<PositionComponent>().Value;

            Assert.InRange(actual.X, expected.X - 1e-6f, expected.X + 1e-6f);
            Assert.InRange(actual.Y, expected.Y - 1e-6f, expected.Y + 1e-6f);
            Assert.InRange(actual.Z, expected.Z - 1e-6f, expected.Z + 1e-6f);
        }
    }
}