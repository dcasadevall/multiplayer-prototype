using System.Numerics;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Shared.Prediction
{
    /// <summary>
    /// Marks that the entity's position is derived locally on the client and should not be replicated continuously.
    /// Holds the spawn position from which derivation starts.
    /// TODO: Eventually we should create a more generic runtime system to handle derived components like this,
    /// </summary>
    public class DerivedPositionComponent : INonReplicatedComponent
    {
        public Vector3 Position { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            // Non-replicated: no-op
        }

        public void Deserialize(IComponentReader reader)
        {
            // Non-replicated: no-op
        }
    }
}