using Server.ECS.Components;
using Shared.ECS;
using Shared.ECS.Simulation;

namespace Server.ECS.Systems;

[TickInterval(50)]
public class ReplicationSystem : ISystem
{
    public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
    {
    }
}