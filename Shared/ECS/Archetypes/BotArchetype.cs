using System.Numerics;
using Shared.Damage;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.Physics;

namespace Shared.ECS.Archetypes
{
    public static class BotArchetype
    {
        public static Entity Create(
            EntityRegistry registry,
            Vector3 spawnPosition)
        {
            var botEntity = registry.CreateEntity();

            // Predicted spatial components
            botEntity.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            botEntity.AddPredictedComponent(new VelocityComponent());

            // Gameplay/state components
            var name = "Bot";
            botEntity.AddComponent(new HealthComponent
            {
                MaxHealth = GameplayConstants.MaxBotHealth,
                CurrentHealth = GameplayConstants.MaxBotHealth
            });

            botEntity.AddComponent(new NameComponent { Name = name });
            botEntity.AddComponent(new PrefabComponent { PrefabName = GameplayConstants.PlayerPrefabName });
            botEntity.AddComponent<BotTagComponent>();
            botEntity.AddComponent<RotationComponent>();
            botEntity.AddComponent(new LocalBoundsComponent
            {
                Center = GameplayConstants.PlayerLocalBoundsCenter,
                Size = GameplayConstants.PlayerLocalBoundsSize
            });
            botEntity.AddComponent<CollidingTagComponent>();

            return botEntity;
        }
    }
}