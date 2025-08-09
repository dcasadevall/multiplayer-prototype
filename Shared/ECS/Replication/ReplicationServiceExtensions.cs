using Microsoft.Extensions.DependencyInjection;

namespace Shared.ECS.Replication
{
    public static class ReplicationServiceExtensions
    {
        public static void RegisterJsonReplicationTypes(this IServiceCollection service)
        {
            service.AddSingleton<IWorldDeltaProducer, JsonWorldDeltaProducer>();
            service.AddSingleton<IWorldDeltaConsumer, JsonWorldDeltaConsumer>();
        }
    }
}