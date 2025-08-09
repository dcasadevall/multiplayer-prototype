using System;
using Shared.ECS;

namespace Server.AI
{
    /// <summary>
    /// TargetComponent is used to identify the target entity for AI agents.
    /// It contains the ID of the target entity that the AI should interact with or focus on.
    /// </summary>
    public class TargetComponent : IServerComponent
    {
        public Guid TargetId { get; set; }
    }
}