using System.Numerics;
using Shared.ECS;
using Shared.Replication;

namespace Server.AI
{
    /// <summary>
    /// A server-side component that stores the state for a bot's roaming behavior.
    /// It is not replicated to clients.
    /// </summary>
    public class RoamingStateComponent : INonReplicatedComponent
    {
        public Vector3 TargetPosition { get; set; }
        public uint NextRoamTick { get; set; }

        public void Serialize(IComponentWriter writer)
        {
        }

        public void Deserialize(IComponentReader reader)
        {
        }
    }
}