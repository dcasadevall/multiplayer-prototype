using System.Numerics;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.ECS.Simulation;
using Shared.Physics;
using Shared.Input;
using System;

namespace Server.AI
{
    /// <summary>
    /// This system controls the behavior of the bots in the game.
    /// It includes logic for chasing and attacking players, as well as retreating when health is low.
    /// When no players are present, bots will roam around randomly.
    /// </summary>
    public class BotAiSystem : ISystem
    {
        private readonly Random _random = new();

        /// <summary>
        /// Updates the state of all bots in the game.
        /// </summary>
        /// <param name="registry">The entity registry.</param>
        /// <param name="tickNumber">The current simulation tick.</param>
        /// <param name="deltaTime">The time since the last tick.</param>
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // We use ToList() to avoid modifying the collection while iterating
            // In a real application, we would want to create a copy of all entities for systems to iterate over
            var players = registry.With<PlayerTagComponent>().ToList();
            foreach (var bot in registry.With<BotTagComponent>().ToList())
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
                        // Randomize the direction, as long as its away from the player
                        direction += new Vector3(
                            Random.Shared.NextSingle() - 0.5f,
                            0,
                            Random.Shared.NextSingle() - 0.5f
                        );

                        bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * ServerConstants.BotRetreatSpeed });
                    }

                    continue;
                }

                // Targeting and Attack logic
                var target = GetOrAcquireTarget(bot, players);
                if (target != null)
                {
                    bot.TryRemove<RoamingStateComponent>(); // Stop roaming when a target is acquired
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
                        // Check for existing velocity to avoid unnecessary replications
                        // This shouldn't be necessary, but with our system we don't currently check
                        // for component equality
                        if (!bot.Has<VelocityComponent>() || bot.GetRequired<VelocityComponent>().Value != Vector3.Zero)
                        {
                            bot.AddOrReplaceComponent(new VelocityComponent { Value = Vector3.Zero });
                        }

                        // Face the target
                        var rotation = Quaternion.CreateFromYawPitchRoll(
                            MathF.Atan2(direction.X, direction.Z),
                            0,
                            0
                        );

                        // Same deal, be conservative about replacing the rotation component
                        if (!bot.Has<RotationComponent>() || bot.GetRequired<RotationComponent>().Value != rotation)
                        {
                            bot.AddOrReplaceComponent(new RotationComponent { Value = rotation });
                        }

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
                else
                {
                    // Roaming logic
                    var roamState = bot.GetOrCreate<RoamingStateComponent>();
                    if (tickNumber >= roamState.NextRoamTick || Vector3.Distance(botPosition, roamState.TargetPosition) < 1f)
                    {
                        // Pick a new random point to roam to
                        var randomDirection = new Vector3((float)_random.NextDouble() * 2 - 1, 0, (float)_random.NextDouble() * 2 - 1);
                        roamState.TargetPosition = botPosition + Vector3.Normalize(randomDirection) * ServerConstants.BoatRoamRadius;
                        roamState.NextRoamTick = tickNumber + ServerConstants.BotRoamInterval.ToNumTicks();
                    }

                    var direction = Vector3.Normalize(roamState.TargetPosition - botPosition);
                    bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * 1.5f });

                    var rotation = Quaternion.CreateFromYawPitchRoll(
                        MathF.Atan2(direction.X, direction.Z), 0, 0);
                    bot.AddOrReplaceComponent(new RotationComponent { Value = rotation });
                }
            }
        }

        private Entity? GetOrAcquireTarget(Entity bot, List<Entity> players)
        {
            if (players.Count == 0) return null;

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