using Microsoft.Extensions.DependencyInjection;

namespace Shared.Networking.Replication;

public static class ReplicationServiceExtensions
{
    public static void RegisterJsonReplicationTypes(this IServiceCollection service)
    {
        service.AddSingleton<IWorldSnapshotProducer, JsonWorldSnapshotProducer>();
        service.AddSingleton<IWorldSnapshotConsumer, JsonWorldSnapshotConsumer>();
    }
}