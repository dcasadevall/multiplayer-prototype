using Server.ECS.Components;
using Shared.ECS;

namespace Server.ECS.Systems;

public class ReplicationSystem : ISystem
{
    public Type[] ObservedComponents =>
    [
        typeof(ReplicatedEntity)
    ];

    public void Update(EntityManager manager, float deltaTime)
    {
        throw new NotImplementedException();
    }
}