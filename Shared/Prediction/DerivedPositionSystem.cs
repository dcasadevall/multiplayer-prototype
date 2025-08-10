using Shared.ECS;
using Shared.ECS.Entities;
using Shared.Physics;

namespace Shared.Prediction
{
    /// <summary>
    /// Ensures that any entity with a <see cref="DerivedPositionComponent"/> also has a <see cref="PositionComponent"/>,
    /// and updates the <see cref="PositionComponent"/> value every tick to match the derived position.
    /// </summary>
    public class DerivedPositionSystem : ISystem
    {
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            foreach (var entity in registry.With<DerivedPositionComponent>())
            {
                var derived = entity.GetRequired<DerivedPositionComponent>();
                if (entity.TryGet<PositionComponent>(out var pos))
                {
                    // This position should not be replicated, so we can mutate it
                    pos.Value = derived.Position;
                }
                else
                {
                    entity.AddComponent(new PositionComponent { Value = derived.Position });
                }
            }
        }
    }
}