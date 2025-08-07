using System.Collections.Generic;
using Core.ECS.Rendering;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.Logging;
using UnityEngine;
using VolumetricLines;
using ILogger = Shared.Logging.ILogger;

namespace Adapters.Rendering
{
    public class ProjectileRenderSystem : ISystem
    {
        private readonly IEntityViewRegistry _entityViewRegistry;
        private readonly ILogger _logger;
        private readonly Dictionary<EntityId, VolumetricLineBehavior> _projectileViews = new();
        private const float TrailLength = 2.0f;
        private const float LineWidth = 0.2f;

        public ProjectileRenderSystem(IEntityViewRegistry entityViewRegistry, ILogger logger)
        {
            _entityViewRegistry = entityViewRegistry;
            _logger = logger;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Find all projectiles
            foreach (var entity in registry.WithAll<ProjectileTagComponent, PositionComponent, VelocityComponent>())
            {
                var entityId = entity.Id;
                var position = entity.GetRequired<PositionComponent>().Value;
                var velocity = entity.GetRequired<VelocityComponent>().Value;
                
                if (!_entityViewRegistry.TryGetEntityView(entityId, out var entityView))
                {
                    _logger.Warn(LoggedFeature.Game, 
                        $"ProjectileRenderSystem: Entity {entityId} does not have a view registered. Skipping rendering.");
                    continue;
                }

                // Add the VolumetricLineBehavior if it doesn't exist
                // This is the visual representation of the projectile's trail
                if (!_projectileViews.ContainsKey(entityId))
                {
                    var line = entityView.gameObject.AddComponent<VolumetricLineBehavior>();
                    line.TemplateMaterial = Resources.Load<Material>("LineStrip-LightSaber");
                    line.LineColor = Color.yellow;
                    line.LineWidth = LineWidth;
                    line.LightSaberFactor = 0.5f;
                    _projectileViews[entityId] = line;
                }

                var endPos = new Vector3(position.X, position.Y, position.Z);
                var startPos = endPos - (Vector3.Normalize(new Vector3(velocity.X, velocity.Y, velocity.Z)) * TrailLength);
                _projectileViews[entityId].StartPos = startPos;
                _projectileViews[entityId].EndPos = endPos;
            }
        }
    }
}
