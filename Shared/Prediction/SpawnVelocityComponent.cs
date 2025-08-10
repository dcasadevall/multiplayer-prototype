using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.Prediction
{
    /// <summary>
    /// Carries initial velocity and the spawn tick for locally-derived kinematic motion.
    /// Replicated once on spawn; clients derive continuous motion locally.
    /// Entities with this component are expected to have a <see cref="DerivedPositionComponent"/>,
    /// which will be updated by the client based on this velocity.
    /// </summary>
    public class SpawnVelocityComponent : IComponent
    {
        private const float QuantizationStep = 0.01f; // centimeter precision in meters

        public Vector3 SpawnPosition { get; set; }
        public uint SpawnTick { get; set; }
        public Vector3 Velocity { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(SpawnTick);
            writer.PutVector3Q(Velocity, QuantizationStep);
            writer.PutVector3Q(SpawnPosition, QuantizationStep);
        }

        public void Deserialize(IComponentReader reader)
        {
            SpawnTick = reader.GetUInt();
            Velocity = reader.GetVector3Q(QuantizationStep);
            SpawnPosition = reader.GetVector3Q(QuantizationStep);
        }
    }
}