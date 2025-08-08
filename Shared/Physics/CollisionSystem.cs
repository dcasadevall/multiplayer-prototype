using System.Collections.Generic;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;

namespace Shared.Physics
{
    /// <summary>
    /// A very basic, not optimized collision detection system.
    /// This system detects collisions between entities that have a <see cref="WorldAABBComponent"/>.
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

            var collidableEntities = registry.WithAll<WorldAABBComponent, CollidingTagComponent>().ToList();
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
            var boxA = a.GetRequired<WorldAABBComponent>();
            var boxB = b.GetRequired<WorldAABBComponent>();

            return (boxA.Min.X <= boxB.Max.X && boxA.Max.X >= boxB.Min.X) &&
                   (boxA.Min.Y <= boxB.Max.Y && boxA.Max.Y >= boxB.Min.Y) &&
                   (boxA.Min.Z <= boxB.Max.Z && boxA.Max.Z >= boxB.Min.Z);
        }

        private void AddIntersection(EntityId source, EntityId target)
        {
            if (!_intersections.ContainsKey(source))
            {
                _intersections[source] = new List<EntityId>();
            }

            _intersections[source].Add(target);
        }

        // <inheritdoc />
        public bool AreColliding(EntityId firstEntity, EntityId secondEntity)
        {
            return _intersections.TryGetValue(firstEntity, out var targets) && targets.Contains(secondEntity);
        }

        // <inheritdoc />
        public List<EntityId> GetCollisionsFor(EntityId entityId)
        {
            if (_intersections.TryGetValue(entityId, out var collisions))
            {
                return collisions;
            }

            return new List<EntityId>();
        }
    }
}