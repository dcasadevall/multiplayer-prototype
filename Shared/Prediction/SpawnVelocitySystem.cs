using Shared.ECS;
using Shared.ECS.Entities;

namespace Shared.Prediction
{
    /// <summary>
    /// Derives position locally from spawn kinematics for entities that declare a <see cref="SpawnVelocityComponent"/>.
    ///
    /// <para>
    /// This system updates <see cref="DerivedPositionComponent"/>.Position each fixed tick using:
    /// Position = SpawnPosition + Velocity * elapsedTime,
    /// where elapsedTime = (currentTick - SpawnTick) * FixedDeltaTime.
    /// </para>
    ///
    /// <para>
    /// This enables bandwidth savings by sending only initial spawn data and deriving motion locally thereafter.
    /// </para>
    /// </summary>
    public class SpawnVelocitySystem : ISystem
    {
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            foreach (var entity in registry.With<SpawnVelocityComponent>())
            {
                // Ensure the entity has a DerivedPositionComponent to update
                if (!entity.Has<DerivedPositionComponent>())
                {
                    entity.AddComponent<DerivedPositionComponent>();
                }

                var spawn = entity.GetRequired<SpawnVelocityComponent>();
                var derived = entity.GetRequired<DerivedPositionComponent>();

                var elapsedTicks = tickNumber > spawn.SpawnTick ? tickNumber - spawn.SpawnTick : 0u;
                var elapsedTime = elapsedTicks * (float)SharedConstants.FixedDeltaTime.TotalSeconds;

                // Ok to mutate component as it is not replicated
                derived.Position = spawn.SpawnPosition + spawn.Velocity * elapsedTime;
            }
        }
    }
}