using System.Numerics;
using Shared.Damage;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Physics;
using Shared.Prediction;
using Shared.Settings;

namespace Shared.ECS.Archetypes
{
    /// <summary>
    /// Defines the complete set of components that a Player entity should have.
    /// </summary>
    public class PlayerFactory
    {
        private readonly EntityRegistry _registry;
        private readonly PlayerSettings _settings;

        public PlayerFactory(EntityRegistry registry, PlayerSettings settings)
        {
            _registry = registry;
            _settings = settings;
        }

        /// <summary>
        /// Creates a new player entity with all required components.
        /// </summary>
        public Entity Create(
            int peerId,
            Vector3 spawnPosition)
        {
            var playerEntity = _registry.CreateEntity();

            // Predicted spatial components
            playerEntity.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            playerEntity.AddPredictedComponent(new VelocityComponent());

            // Gameplay/state components
            var name = $"Player_{peerId}";
            playerEntity.AddComponent(new HealthComponent
            {
                MaxHealth = _settings.MaxPlayerHealth,
                CurrentHealth = _settings.MaxPlayerHealth
            });

            playerEntity.AddComponent(new PeerComponent { PeerId = peerId, PeerName = name });
            playerEntity.AddComponent(new NameComponent { Name = name });
            playerEntity.AddComponent(new PrefabComponent { PrefabName = _settings.PlayerPrefabName });
            playerEntity.AddComponent<PlayerTagComponent>();
            playerEntity.AddComponent(new RotationComponent());
            playerEntity.AddComponent(new LocalBoundsComponent
            {
                Center = _settings.PlayerLocalBoundsCenter,
                Size = _settings.PlayerLocalBoundsSize
            });
            playerEntity.AddComponent<CollidingTagComponent>();

            playerEntity.AddComponent(ColorComponent.RandomColor());

            return playerEntity;
        }
    }
}