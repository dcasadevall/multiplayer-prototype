using System.Numerics;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Physics;
using Shared.Input;
using Shared.Respawn;

namespace Server.AI
{
    /// <summary>
    /// This system controls the behavior of the bots in the game.
    /// It includes logic for chasing and attacking players, as well as retreating when health is low.
    /// </summary>
    public class BotAiSystem : ISystem
    {
        /// <summary>
        /// Updates the state of all bots in the game.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">The current simulation tick.</param>
        /// <param name="deltaTime">The time since the last tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var players = registry.With<PlayerTagComponent>().ToList();
            if (players.Count == 0)
                return;

            var bots = registry.With<BotTagComponent>().ToList();
            foreach (var bot in bots)
            {
                var botHealth = bot.GetRequired<HealthComponent>();
                var botPosition = bot.GetRequired<PositionComponent>().Value;

                // Retreat logic
                if ((float)botHealth.CurrentHealth / botHealth.MaxHealth < ServerConstants.BotRetreatHealthPercentThreshold)
                {
                    // Find a safe spot to run to (e.g., away from the nearest player)
                    var nearestPlayer = FindClosestPlayer(botPosition, players);
                    if (nearestPlayer != null)
                    {
                        var playerPosition = nearestPlayer.GetRequired<PositionComponent>().Value;
                        var direction = Vector3.Normalize(botPosition - playerPosition);
                        bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * 3f });
                    }

                    continue;
                }

                // Targeting and Attack logic
                var target = GetOrAcquireTarget(bot, players);
                if (target != null)
                {
                    var targetPosition = target.GetRequired<PositionComponent>().Value;
                    var direction = Vector3.Normalize(targetPosition - botPosition);
                    var distance = Vector3.Distance(botPosition, targetPosition);

                    if (distance > ServerConstants.BotAttackDistance)
                    {
                        bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * 2f });
                    }
                    else
                    {
                        // Stop moving when in attack range
                        bot.AddOrReplaceComponent(new VelocityComponent { Value = Vector3.Zero });

                        // Face the target
                        var rotation = Quaternion.CreateFromYawPitchRoll(
                            MathF.Atan2(direction.X, direction.Z),
                            0,
                            0
                        );
                        bot.AddOrReplaceComponent(new RotationComponent { Value = rotation });

                        // Shoot
                        if (!bot.Has<ShootingCooldownComponent>() || tickNumber >= bot.GetRequired<ShootingCooldownComponent>().EndTick)
                        {
                            bot.AddOrReplaceComponent(new ShootingCooldownComponent
                            {
                                EndTick = tickNumber + ServerConstants.BotShootingCooldown.ToNumTicks()
                            });

                            // Shoot the thing :)
                            ProjectileArchetype.CreateFromEntity(registry, bot, tickNumber);
                        }
                    }
                }
            }
        }

        private Entity? GetOrAcquireTarget(Entity bot, List<Entity> players)
        {
            if (bot.Has<TargetComponent>())
            {
                var targetId = bot.GetRequired<TargetComponent>().TargetId;
                var target = players.FirstOrDefault(p => p.Id.Value == targetId);
                if (target != null)
                {
                    return target;
                }
            }

            var closestPlayer = FindClosestPlayer(bot.GetRequired<PositionComponent>().Value, players);
            if (closestPlayer == null)
            {
                // No players found, return a default entity or handle accordingly
                return null;
            }

            bot.AddOrReplaceComponent(new TargetComponent { TargetId = closestPlayer.Id.Value });

            return closestPlayer;
        }

        private Entity? FindClosestPlayer(Vector3 position, List<Entity> players)
        {
            Entity? closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var player in players)
            {
                var playerPosition = player.GetRequired<PositionComponent>().Value;
                var distance = Vector3.DistanceSquared(position, playerPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer;
        }
    }
}