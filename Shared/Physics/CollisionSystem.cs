using System.Collections.Generic;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;

namespace Shared.Physics
{
    /// <summary>
    /// A very basic, not optimized collision detection system.
    /// This system detects collisions between entities that have a <see cref="BoxColliderComponent"/>.
    /// It checks for intersections between the bounding boxes of entities and stores the results.
    ///
    /// <param>
    /// WARNING: This system runs at O(n^2) complexity, meaning it checks every pair of collidable entities.
    /// This is not suitable for large numbers of entities or high-frequency updates.
    /// It is however a good starting point for simple games or prototypes.
    /// </param>
    /// </summary>
    public class CollisionSystem : ISystem, ICollisionDetector
    {
        private readonly Dictionary<EntityId, List<EntityId>> _intersections = new();

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            _intersections.Clear();

            var collidableEntities = registry.With<BoxColliderComponent>().ToList();
            for (int i = 0; i < collidableEntities.Count; i++)
            {
                for (int j = i + 1; j < collidableEntities.Count; j++)
                {
                    var entityA = collidableEntities[i];
                    var entityB = collidableEntities[j];

                    if (IsIntersecting(entityA, entityB))
                    {
                        AddIntersection(entityA.Id, entityB.Id);
                        AddIntersection(entityB.Id, entityA.Id);
                    }
                }
            }
        }

        private bool IsIntersecting(Entity a, Entity b)
        {
            var posA = a.Has<PositionComponent>() ? a.GetRequired<PositionComponent>().Value : System.Numerics.Vector3.Zero;
            var colA = a.GetRequired<BoxColliderComponent>();
            var minA = posA + colA.Center - colA.Size / 2;
            var maxA = posA + colA.Center + colA.Size / 2;

            var posB = b.Has<PositionComponent>() ? b.GetRequired<PositionComponent>().Value : System.Numerics.Vector3.Zero;
            var colB = b.GetRequired<BoxColliderComponent>();
            var minB = posB + colB.Center - colB.Size / 2;
            var maxB = posB + colB.Center + colB.Size / 2;

            return (minA.X <= maxB.X && maxA.X >= minB.X) &&
                   (minA.Y <= maxB.Y && maxA.Y >= minB.Y) &&
                   (minA.Z <= maxB.Z && maxA.Z >= minB.Z);
        }

        private void AddIntersection(EntityId source, EntityId target)
        {
            if (!_intersections.ContainsKey(source))
            {
                _intersections[source] = new List<EntityId>();
            }

            _intersections[source].Add(target);
        }

        public bool AreColliding(EntityId firstEntity, EntityId secondEntity)
        {
            return _intersections.TryGetValue(firstEntity, out var targets) && targets.Contains(secondEntity);
        }
    }
}