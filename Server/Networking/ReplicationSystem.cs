using Shared.ECS;
using Shared.ECS.Simulation;

namespace Server.Networking;

[TickInterval(50)]
public class ReplicationSystem : ISystem
{
    public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
    {
        throw new NotImplementedException();
    }
}