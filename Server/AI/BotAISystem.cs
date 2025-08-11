using System.Numerics;
using Shared.Damage;
using Shared.ECS;
using Shared.ECS.Archetypes;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using Shared.Logging;
using Shared.Physics;
using Shared.Settings;

namespace Server.AI
{
    /// <summary>
    /// This system controls the behavior of the bots in the game.
    /// It includes logic for chasing and attacking players, as well as retreating when health is low.
    /// When no players are present, bots will stand still.
    /// </summary>
    public class BotAiSystem(
        BotSettings botSettings,
        ProjectileFactory projectileFactory,
        SimulationSettings simulationSettings,
        ILogger logger) : ISystem
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
            var bots = registry.With<BotTagComponent>().ToList();

            foreach (var bot in bots)
            {
                var botHealth = bot.GetRequired<HealthComponent>();

                // Skip dead bots
                if (botHealth.CurrentHealth <= 0)
                {
                    continue;
                }

                if (botHealth.MaxHealth <= 0)
                {
                    logger.Warn(LoggedFeature.Game, $"Bot {bot.Id} has zero max health, skipping AI update.");
                    continue;
                }

                if ((float)botHealth.CurrentHealth / botHealth.MaxHealth < botSettings.BotRetreatHealthPercentThreshold)
                {
                    HandleRetreatState(bot, players);
                    continue;
                }

                // Scatter when no target (no players)
                if (players.Count == 0)
                {
                    HandleScatterState(bot, bots);
                    continue;
                }

                HandleAttackState(bot, players, tickNumber);
            }
        }

        private void HandleRetreatState(Entity bot, List<Entity> players)
        {
            var botPosition = bot.GetRequired<PositionComponent>().Value;
            var nearestPlayer = FindClosestPlayer(botPosition, players);
            if (nearestPlayer != null)
            {
                var playerPosition = nearestPlayer.GetRequired<PositionComponent>().Value;
                var direction = Vector3.Normalize(botPosition - playerPosition);
                direction += new Vector3(
                    Random.Shared.NextSingle() - 0.5f,
                    0,
                    Random.Shared.NextSingle() - 0.5f
                );

                bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * botSettings.BotRetreatSpeed });
            }
        }

        private void HandleScatterState(Entity bot, List<Entity> allBots)
        {
            var myPos = bot.GetRequired<PositionComponent>().Value;

            // Parameters: reuse existing settings to avoid adding new ones
            var maxGroupRadius = botSettings.BoatRoamRadius; // soft boundary for group
            var desiredSeparation = System.MathF.Max(0.5f, maxGroupRadius * 0.4f);

            // Compute centroid of all bots to keep group bounded
            Vector3 centroid = Vector3.Zero;
            int count = 0;
            foreach (var b in allBots)
            {
                centroid += b.GetRequired<PositionComponent>().Value;
                count++;
            }
            if (count > 0) centroid /= count;

            // Separation: push away from nearby bots
            Vector3 separation = Vector3.Zero;
            foreach (var b in allBots)
            {
                if (b.Id.Value == bot.Id.Value) continue;
                var otherPos = b.GetRequired<PositionComponent>().Value;
                var toMe = myPos - otherPos;
                var dist = toMe.Length();
                if (dist > 1e-4f && dist < desiredSeparation)
                {
                    separation += Vector3.Normalize(toMe) * ((desiredSeparation - dist) / desiredSeparation);
                }
            }

            // Cohesion limiter: if too far from centroid, gently steer back
            Vector3 cohesion = Vector3.Zero;
            var fromCenter = myPos - centroid;
            var distFromCenter = fromCenter.Length();
            if (distFromCenter > maxGroupRadius)
            {
                cohesion = -Vector3.Normalize(fromCenter) * System.MathF.Min(1f, (distFromCenter - maxGroupRadius) / maxGroupRadius);
            }

            var steer = separation + 0.5f * cohesion;
            if (steer.LengthSquared() < 1e-6f)
            {
                // No steering required; slow down
                if (!bot.Has<VelocityComponent>() || bot.GetRequired<VelocityComponent>().Value != Vector3.Zero)
                {
                    bot.AddOrReplaceComponent(new VelocityComponent { Value = Vector3.Zero });
                }
                return;
            }

            var moveDir = Vector3.Normalize(steer);
            bot.AddOrReplaceComponent(new VelocityComponent { Value = moveDir * botSettings.BotApproachSpeed });

            // Face movement direction
            var rotation = Quaternion.CreateFromYawPitchRoll(System.MathF.Atan2(moveDir.X, moveDir.Z), 0, 0);
            if (!bot.Has<RotationComponent>() || bot.GetRequired<RotationComponent>().Value != rotation)
            {
                bot.AddOrReplaceComponent(new RotationComponent { Value = rotation });
            }
        }

        private void HandleAttackState(Entity bot, List<Entity> players, uint tickNumber)
        {
            var target = GetOrAcquireTarget(bot, players);
            if (target != null)
            {
                var botPosition = bot.GetRequired<PositionComponent>().Value;
                var targetPosition = target.GetRequired<PositionComponent>().Value;
                var direction = Vector3.Normalize(targetPosition - botPosition);
                var distance = Vector3.Distance(botPosition, targetPosition);

                if (distance > botSettings.BotAttackDistance)
                {
                    // Move towards target
                    bot.AddOrReplaceComponent(new VelocityComponent { Value = direction * botSettings.BotApproachSpeed });

                    // Face movement direction while approaching
                    var approachRotation = Quaternion.CreateFromYawPitchRoll(System.MathF.Atan2(direction.X, direction.Z), 0, 0);
                    if (!bot.Has<RotationComponent>() || bot.GetRequired<RotationComponent>().Value != approachRotation)
                    {
                        bot.AddOrReplaceComponent(new RotationComponent { Value = approachRotation });
                    }
                }
                else
                {
                    if (!bot.Has<VelocityComponent>() || bot.GetRequired<VelocityComponent>().Value != Vector3.Zero)
                    {
                        bot.AddOrReplaceComponent(new VelocityComponent { Value = Vector3.Zero });
                    }

                    var rotation = Quaternion.CreateFromYawPitchRoll(System.MathF.Atan2(direction.X, direction.Z), 0, 0);
                    if (!bot.Has<RotationComponent>() || bot.GetRequired<RotationComponent>().Value != rotation)
                    {
                        bot.AddOrReplaceComponent(new RotationComponent { Value = rotation });
                    }

                    if (!bot.Has<ShootingCooldownComponent>() || tickNumber >= bot.GetRequired<ShootingCooldownComponent>().EndTick)
                    {
                        bot.AddOrReplaceComponent(new ShootingCooldownComponent
                        {
                            EndTick = tickNumber + (uint)(botSettings.BotShootingCooldown.TotalSeconds *
                                                          simulationSettings.WorldTicksPerSecond)
                        });

                        projectileFactory.CreateFromEntity(bot, tickNumber);
                    }
                }
            }
            else
            {
                if (!bot.Has<VelocityComponent>() || bot.GetRequired<VelocityComponent>().Value != Vector3.Zero)
                {
                    bot.AddOrReplaceComponent(new VelocityComponent { Value = Vector3.Zero });
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