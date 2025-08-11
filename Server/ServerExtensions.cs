using Microsoft.Extensions.DependencyInjection;
using Server.AI;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Networking;
using Shared.Physics;
using Shared.Replication;
using Shared.Respawn;
using Shared.Scheduling;
using Server.Player;
using Server.Scenes;
using Shared;
using Shared.Damage;
using Shared.ECS.Archetypes;
using Shared.ECS.Entities;
using Shared.ECS.TickSync;
using Shared.Settings;
using Server.Health;

namespace Server
{
    public static class ServerExtensions
    {
        public static void RegisterServerTypes(this IServiceCollection services)
        {
            // Add settings validation
            services.AddSingleton<SettingsValidator>();
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<SettingsValidator>());

            // Register Entity Registry and factories
            services.AddSingleton<EntityRegistry>();
            services.AddSingleton<PlayerFactory>();
            services.AddSingleton<ProjectileFactory>();
            services.AddSingleton<BotFactory>();

            // Register server systems
            services.AddSingleton<ISystem, WorldDiagnosticsSystem>();
            services.AddSingleton<ISystem, HealthSystem>();
            services.AddSingleton<ISystem, WorldAABBUpdateSystem>();
            services.AddSingleton<ISystem, VelocitySystem>();
            services.AddSingleton<CollisionSystem>();
            services.AddSingleton<ISystem>(sp => sp.GetRequiredService<CollisionSystem>());
            services.AddSingleton<ICollisionDetector>(sp => sp.GetRequiredService<CollisionSystem>());
            services.AddSingleton<ISystem, UnitCollisionSystem>();
            services.AddSingleton<ISystem, DamageSystem>();
            services.AddSingleton<ISystem, DeathSystem>();
            services.AddSingleton<ISystem, RespawnSystem>();
            services.AddSingleton<ISystem, BotAiSystem>();

            // Register TickSync and ServerTickSystem
            var tickSync = new TickSync();
            services.AddSingleton<ISystem>(_ => new ServerTickSystem(tickSync));
            services.AddSingleton<ITickSync>(tickSync);

            // Server replication. Should be the last system registered
            services.AddSingleton<ServerReplicationSystem>();
            services.AddSingleton<ISystem>(sp => sp.GetRequiredService<ServerReplicationSystem>());
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<ServerReplicationSystem>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<ServerReplicationSystem>());
            services.AddSingleton<IWorldSnapshotProvider>(sp => sp.GetRequiredService<ServerReplicationSystem>());

            // Scene loading
            services.AddSingleton<SceneLoader>();

            // Entity lifecycle management
            services.AddSingleton<PlayerSpawnHandler>();
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<PlayerSpawnHandler>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<PlayerSpawnHandler>());
            services.AddSingleton<ISystem, SelfDestroyingSystem>();

            // Register message sender and receiver
            services.AddSingleton<IMessageSender, NetLibBinaryMessageSender>();
            services.AddSingleton<NetLibBinaryMessageReceiver>();
            services.AddSingleton<IMessageReceiver>(sp => sp.GetRequiredService<NetLibBinaryMessageReceiver>());
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<NetLibBinaryMessageReceiver>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<NetLibBinaryMessageReceiver>());

            // The scheduler is server specific
            services.AddSingleton<IScheduler, TimerScheduler>();

            // Register the networking server abstraction
            services.AddSingleton<INetworkingServer, NetLibNetworkingServer>();

            // Shared types registration
            services.RegisterSharedTypes();

            // Server Input handling
            services.AddSingleton<PlayerMovementHandler>();
            services.AddSingleton<IInitializable, PlayerMovementHandler>();
            services.AddSingleton<IDisposable, PlayerMovementHandler>();
            services.AddSingleton<PlayerShotHandler>();
            services.AddSingleton<IInitializable, PlayerShotHandler>();
            services.AddSingleton<IDisposable, PlayerShotHandler>();

            // Heroku HTTP health server
            services.AddSingleton<HttpHealthServer>();
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<HttpHealthServer>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<HttpHealthServer>());
        }
    }
}