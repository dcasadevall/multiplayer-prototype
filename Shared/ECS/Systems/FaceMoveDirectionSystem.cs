using System;
using System.Linq;
using System.Numerics;
using Shared.ECS.Components;

namespace Shared.ECS.Systems
{
    public class FaceMoveDirectionSystem : ISystem
    {
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            foreach (var entity in registry.WithAll<VelocityComponent, RotationComponent>())
            {
                var velocity = entity.GetRequired<VelocityComponent>().Value;
                if (velocity == Vector3.Zero) continue;

                var direction = Vector3.Normalize(velocity);
                var rotation = Quaternion.CreateFromYawPitchRoll(
                    MathF.Atan2(direction.X, direction.Z),
                    0,
                    0
                );

                entity.GetRequired<RotationComponent>().Value = rotation;
            }
        }
    }
}