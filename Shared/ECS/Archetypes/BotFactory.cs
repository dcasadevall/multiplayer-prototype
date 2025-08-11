using System.Numerics;
using Shared.Damage;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Physics;
using Shared.Prediction;
using Shared.Settings;

namespace Shared.ECS.Archetypes
{
    public class BotFactory
    {
        private readonly EntityRegistry _registry;
        private readonly BotSettings _settings;

        public BotFactory(EntityRegistry registry, BotSettings settings)
        {
            _registry = registry;
            _settings = settings;
        }

        public Entity Create(Vector3 spawnPosition)
        {
            var botEntity = _registry.CreateEntity();

            // Predicted spatial components
            botEntity.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            botEntity.AddPredictedComponent(new VelocityComponent());

            // Gameplay/state components
            var name = "Bot";
            botEntity.AddComponent(new HealthComponent
            {
                MaxHealth = _settings.MaxBotHealth,
                CurrentHealth = _settings.MaxBotHealth
            });

            botEntity.AddComponent(new NameComponent { Name = name });
            botEntity.AddComponent(new PrefabComponent { PrefabName = _settings.PrefabName });
            botEntity.AddComponent<BotTagComponent>();
            botEntity.AddComponent<RotationComponent>();
            botEntity.AddComponent(new LocalBoundsComponent
            {
                Center = _settings.LocalBoundsCenter,
                Size = _settings.LocalBoundsSize
            });
            botEntity.AddComponent<CollidingTagComponent>();
            botEntity.AddComponent(ColorComponent.RandomColor());

            return botEntity;
        }
    }
}