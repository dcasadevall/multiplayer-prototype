using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Respawn;
using Shared.Settings;

namespace Shared.Damage
{
    /// <summary>
    /// This system is responsible for handling the death of entities.
    /// It identifies entities with zero or less health, creates a death record for them,
    /// and then destroys the original entity.
    /// </summary>
    public class DeathSystem : ISystem
    {
        private readonly PlayerSettings _playerSettings;
        private readonly BotSettings _botSettings;
        private readonly SimulationSettings _simulationSettings;

        public DeathSystem(PlayerSettings playerSettings, BotSettings botSettings, SimulationSettings simulationSettings)
        {
            _playerSettings = playerSettings;
            _botSettings = botSettings;
            _simulationSettings = simulationSettings;
        }

        /// <summary>
        /// Identifies dead entities, creates death records, and destroys the original entities.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">The current simulation tick.</param>
        /// <param name="deltaTime">The time since the last tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var deadEntities = registry
                .With<HealthComponent>()
                .Where(e => e.GetRequired<HealthComponent>().CurrentHealth <= 0)
                .ToList();

            foreach (var entity in deadEntities)
            {
                var deathRecord = registry.CreateEntity();
                if (entity.Has<PlayerTagComponent>())
                {
                    deathRecord.AddComponent(new RespawnComponent
                    {
                        RespawnAtTick = tickNumber + _playerSettings.PlayerRespawnTime.ToNumTicks(_simulationSettings.WorldTicksPerSecond)
                    });

                    deathRecord.AddComponent(entity.GetRequired<PeerComponent>());
                }
                else if (entity.Has<BotTagComponent>())
                {
                    deathRecord.AddComponent(new RespawnComponent
                    {
                        RespawnAtTick = tickNumber + _botSettings.BotRespawnTime.ToNumTicks(_simulationSettings.WorldTicksPerSecond)
                    });
                }

                registry.DestroyEntity(entity.Id);
            }
        }
    }
}