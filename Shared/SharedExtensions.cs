using Microsoft.Extensions.DependencyInjection;
using Shared.ECS.Components;
using Shared.Networking;
using Shared.Networking.Replication;
using Shared.Scheduling;

namespace Shared;

public static class SharedExtensions
{
    public static void RegisterSharedTypes(this IServiceCollection serviceCollection)
    {
        serviceCollection.RegisterSchedulingTypes();
        serviceCollection.RegisterNetLibTypes();
        serviceCollection.RegisterJsonReplicationTypes();
    }
}