using System.Numerics;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Prediction;
using Shared.ECS.Replication;
using Shared.Health;
using Shared.Physics;

namespace Shared.ECS.Archetypes
{
    /// <summary>
    /// Defines the complete set of components that a Player entity should have.
    /// </summary>
    public static class PlayerArchetype
    {
        /// <summary>
        /// Creates a new player entity with all required components.
        /// </summary>
        public static Entity Create(
            EntityRegistry registry,
            int peerId,
            Vector3 spawnPosition)
        {
            var playerEntity = registry.CreateEntity();

            // Predicted spatial components
            playerEntity.AddPredictedComponent(new PositionComponent { Value = spawnPosition });
            playerEntity.AddPredictedComponent(new VelocityComponent());

            // Gameplay/state components
            var name = $"Player_{peerId}";
            playerEntity.AddComponent(new HealthComponent { MaxHealth = 100, CurrentHealth = 100 });
            playerEntity.AddComponent(new PeerComponent { PeerId = peerId, PeerName = name });
            playerEntity.AddComponent(new NameComponent { Name = name });
            playerEntity.AddComponent(new PrefabComponent { PrefabName = GameplayConstants.PlayerPrefabName });
            playerEntity.AddComponent<PlayerTagComponent>();
            playerEntity.AddComponent(new RotationComponent());
            playerEntity.AddComponent(new BoxColliderComponent
            {
                Center = GameplayConstants.PlayerColliderBoxCenter,
                Size = GameplayConstants.PlayerColliderBoxSize
            });

            // Network replication
            playerEntity.AddComponent<ReplicatedTagComponent>();

            return playerEntity;
        }
    }
}