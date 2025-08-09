using Microsoft.Extensions.DependencyInjection;
using Shared.ECS.Replication;
using Shared.Networking;
using Shared.Scheduling;

namespace Shared
{
    public static class SharedExtensions
    {
        public static void RegisterSharedTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.RegisterSchedulingTypes();
            serviceCollection.RegisterNetLibTypes();
        }
    }
}