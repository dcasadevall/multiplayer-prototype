using Shared.Damage;
using Shared.ECS.Entities;
using Xunit;

namespace SharedUnitTests.Damage
{
    public class HealthSystemTests
    {
        [Fact]
        public void Update_HealsUpToMaxHealth_WhenBelowMax()
        {
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 90 });

            var system = new HealthSystem();

            // HealthSystem has TickInterval(10); simulate one invocation
            system.Update(registry, 10u, 0f);

            var health = entity.GetRequired<HealthComponent>();
            Assert.Equal(95, health.CurrentHealth); // +5 regen per tick per system run
            Assert.Equal(100, health.MaxHealth);
        }

        [Fact]
        public void Update_DoesNotExceedMaxHealth()
        {
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 99 });

            var system = new HealthSystem();
            system.Update(registry, 10u, 0f);

            var health = entity.GetRequired<HealthComponent>();
            Assert.Equal(100, health.CurrentHealth); // capped at max
        }

        [Fact]
        public void Update_NoChange_WhenAtMaxHealth()
        {
            var registry = new EntityRegistry();
            var entity = registry.CreateEntity();
            entity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 100 });

            var system = new HealthSystem();
            system.Update(registry, 10u, 0f);

            var health = entity.GetRequired<HealthComponent>();
            Assert.Equal(100, health.CurrentHealth);
        }
    }
}
