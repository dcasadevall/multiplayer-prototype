using System.Linq;
using System.Numerics;
using Shared.ECS;

namespace Shared.Physics
{
    /// <summary>
    /// Calculates and updates the world-space axis-aligned bounding box (<see cref="WorldAABBComponent"/>)
    /// for all entities that have position, rotation, and local bounds defined.
    /// This system ensures that the AABB accurately encloses the entity as it moves and rotates in the world.
    /// </summary>
    public class WorldAABBUpdateSystem : ISystem
    {
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            var entities = registry.WithAll<PositionComponent, RotationComponent, LocalBoundsComponent>().ToList();
            foreach (var entity in entities)
            {
                var position = entity.GetRequired<PositionComponent>().Value;
                var rotation = Quaternion.Normalize(entity.GetRequired<RotationComponent>().Value);
                var bounds = entity.GetRequired<LocalBoundsComponent>();

                var halfSize = bounds.Size / 2f;
                var center = bounds.Center;

                var corners = new Vector3[8];
                corners[0] = new Vector3(center.X - halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z);
                corners[1] = new Vector3(center.X + halfSize.X, center.Y - halfSize.Y, center.Z - halfSize.Z);
                corners[2] = new Vector3(center.X - halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z);
                corners[3] = new Vector3(center.X + halfSize.X, center.Y + halfSize.Y, center.Z - halfSize.Z);
                corners[4] = new Vector3(center.X - halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z);
                corners[5] = new Vector3(center.X + halfSize.X, center.Y - halfSize.Y, center.Z + halfSize.Z);
                corners[6] = new Vector3(center.X - halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z);
                corners[7] = new Vector3(center.X + halfSize.X, center.Y + halfSize.Y, center.Z + halfSize.Z);

                var worldMin = new Vector3(float.MaxValue);
                var worldMax = new Vector3(float.MinValue);

                for (int i = 0; i < 8; i++)
                {
                    var worldCorner = Vector3.Transform(corners[i], rotation) + position;
                    worldMin = Vector3.Min(worldMin, worldCorner);
                    worldMax = Vector3.Max(worldMax, worldCorner);
                }

                entity.AddOrReplaceComponent(new WorldAABBComponent
                {
                    Min = worldMin,
                    Max = worldMax,
                });
            }
        }
    }
}