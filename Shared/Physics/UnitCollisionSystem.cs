using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Shared.ECS;
using Shared.ECS.Entities;

namespace Shared.Physics
{
    /// <summary>
    /// Resolves unit-unit collisions by separating overlapping entities based on their world AABBs.
    /// Requires <see cref="WorldAABBComponent"/> and <see cref="CollidingTagComponent"/> to be present.
    ///
    /// Note: Entities tagged with <see cref="DoesNotOccupySpaceTagComponent"/> are ignored by the separation step.
    /// This is a stopgap to allow certain entities (e.g., projectiles) to avoid influencing unit separation.
    /// The ideal long-term solution is a configurable collision matrix in settings to control which categories
    /// collide and/or resolve against each other.
    /// </summary>
    public class UnitCollisionSystem : ISystem
    {
        private readonly ICollisionDetector _collisionDetector;

        public UnitCollisionSystem(ICollisionDetector collisionDetector)
        {
            _collisionDetector = collisionDetector;
        }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            // Work on collidable entities only
            var entities = registry.WithAll<WorldAABBComponent, CollidingTagComponent, PositionComponent>().ToList();
            var handledPairs = new HashSet<(EntityId, EntityId)>();

            foreach (var entity in entities)
            {
                var collisions = _collisionDetector.GetCollisionsFor(entity.Id);
                foreach (var otherId in collisions)
                {
                    // Ensure we only process each pair once
                    var key = entity.Id.Value.CompareTo(otherId.Value) < 0
                        ? (entity.Id, otherId)
                        : (new EntityId(otherId.Value), entity.Id);
                    if (handledPairs.Contains(key)) continue;

                    if (!registry.TryGet(otherId, out var other)) continue;
                    if (!other.Has<PositionComponent>()) continue;

                    // Skip resolution if either side does not occupy space (e.g., projectiles)
                    if (entity.Has<DoesNotOccupySpaceTagComponent>() || other.Has<DoesNotOccupySpaceTagComponent>())
                    {
                        continue;
                    }

                    if (!entity.TryGet(out WorldAABBComponent aabbA) || !other.TryGet(out WorldAABBComponent aabbB))
                        continue;

                    // Compute overlap on each axis
                    var overlapX = GetOverlap(aabbA.Min.X, aabbA.Max.X, aabbB.Min.X, aabbB.Max.X);
                    var overlapY = GetOverlap(aabbA.Min.Y, aabbA.Max.Y, aabbB.Min.Y, aabbB.Max.Y);
                    var overlapZ = GetOverlap(aabbA.Min.Z, aabbA.Max.Z, aabbB.Min.Z, aabbB.Max.Z);

                    if (overlapX <= 0 || overlapY <= 0 || overlapZ <= 0)
                    {
                        continue;
                    }

                    // Move along the smallest penetration axis
                    Vector3 push;
                    var centerA = CenterVec(aabbA);
                    var centerB = CenterVec(aabbB);
                    if (overlapX <= overlapY && overlapX <= overlapZ)
                    {
                        var dir = centerA.X < centerB.X ? -1f : 1f;
                        push = new Vector3(dir * overlapX / 2f, 0, 0);
                    }
                    else if (overlapY <= overlapX && overlapY <= overlapZ)
                    {
                        var dir = centerA.Y < centerB.Y ? -1f : 1f;
                        push = new Vector3(0, dir * overlapY / 2f, 0);
                    }
                    else
                    {
                        var dir = centerA.Z < centerB.Z ? -1f : 1f;
                        push = new Vector3(0, 0, dir * overlapZ / 2f);
                    }

                    var posA = entity.GetRequired<PositionComponent>().Value;
                    var posB = other.GetRequired<PositionComponent>().Value;

                    entity.AddOrReplaceComponent(new PositionComponent { Value = posA + push });
                    other.AddOrReplaceComponent(new PositionComponent { Value = posB - push });

                    handledPairs.Add(key);
                }
            }
        }

        private static Vector3 CenterVec(WorldAABBComponent aabb)
        {
            return (aabb.Min + aabb.Max) * 0.5f;
        }

        private static float GetOverlap(float minA, float maxA, float minB, float maxB)
        {
            var left = maxA - minB;
            var right = maxB - minA;
            return left < right ? left : right;
        }
    }
}