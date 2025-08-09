using System;
using Core.ECS.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.ECS.Replication;
using Shared.ECS.TickSync;
using Shared.Networking;

namespace Core.ECS
{
    /// <summary>
    /// Provides dependency injection registration for ECS-related services only.
    /// 
    /// <para>
    /// <c>EcsExtensions</c> is responsible for registering and managing only the
    /// core ECS (Entity Component System) services required by the client. It does not
    /// handle general game or adapter services, but focuses exclusively on ECS systems,
    /// entity registries, and ECS replication.
    /// </para>
    /// </summary>
    public static class EcsExtensions
    {
        /// <summary>
        /// Registers ECS core services into the provided service collection.
        /// This should be called during initialization to add ECS-specific dependencies.
        /// </summary>
        public static void RegisterEcsServices(this IServiceCollection services)
        {
            // Register Entity Registry
            services.AddSingleton<EntityRegistry>();
            
            // Register core client systems
            // Client replication must go first
            services.AddSingleton<ClientReplicationSystem>();
            services.AddSingleton<ISystem>(sp => sp.GetRequiredService<ClientReplicationSystem>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<ClientReplicationSystem>());
            services.AddSingleton<IReplicationStats>(sp => sp.GetRequiredService<ClientReplicationSystem>());
            
            // After replication, register the tick sync system so it's available for the other
            // systems that need it.
            var tickSync = new TickSync();
            services.AddSingleton<ITickSync>(tickSync);
            services.AddSingleton<ISystem, ClientTickSystem>(sp => new ClientTickSystem(tickSync, sp.GetRequiredService<IClientConnection>()));
            
            // Entity view system creates and manages entity game object creation and destruction
            services.AddSingleton<EntityViewSystem>();
            services.AddSingleton<ISystem>(sp => sp.GetService<EntityViewSystem>());
            services.AddSingleton<IDisposable>(sp => sp.GetService<EntityViewSystem>());
            services.AddSingleton<IEntityViewRegistry>(sp => sp.GetService<EntityViewSystem>());
            
            
            // Register json replication types
            services.RegisterJsonReplicationTypes();
        }
    }
}
