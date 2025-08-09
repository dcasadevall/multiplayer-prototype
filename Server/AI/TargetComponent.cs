using System;
using Shared.ECS;

namespace Server.AI
{
    public class TargetComponent : IComponent
    {
        public Guid TargetId { get; set; }
    }
}

