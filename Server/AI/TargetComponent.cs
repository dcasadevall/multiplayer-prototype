using System;
using Shared.ECS;
using Shared.ECS.Replication;

namespace Server.AI
{
    /// <summary>
    /// TargetComponent is used to identify the target entity for AI agents.
    /// It contains the ID of the target entity that the AI should interact with or focus on.
    /// </summary>
    public class TargetComponent : IServerComponent
    {
        public Guid TargetId { get; set; }

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(TargetId);
        }

        public void Deserialize(IComponentReader reader)
        {
            TargetId = reader.GetGuid();
        }
    }
}