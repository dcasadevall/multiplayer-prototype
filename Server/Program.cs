using Microsoft.Extensions.DependencyInjection;
using Server.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Settings;

namespace Server
{
    internal class Program
    {
        private const string BotSettingsSection = "Settings:BotSettings";
        private const string NetworkSettingsSection = "Settings:NetworkSettings";
        private const string PlayerSettingsSection = "Settings:PlayerSettings";
        private const string ProjectileSettingsSection = "Settings:ProjectileSettings";
        private const string SimulationSettingsSection = "Settings:SimulationSettings";

        private static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration((_, configApp) =>
                {
                    var basePath = AppContext.BaseDirectory;
                    configApp.SetBasePath(basePath);
                    configApp.AddJsonFile("appsettings.json", optional: false);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Configure settings
                    services.Configure<BotSettings>(hostContext.Configuration.GetSection(BotSettingsSection));
                    services.Configure<NetworkSettings>(hostContext.Configuration.GetSection(NetworkSettingsSection));
                    services.Configure<PlayerSettings>(hostContext.Configuration.GetSection(PlayerSettingsSection));
                    services.Configure<ProjectileSettings>(hostContext.Configuration.GetSection(ProjectileSettingsSection));
                    services.Configure<SimulationSettings>(hostContext.Configuration.GetSection(SimulationSettingsSection));

                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<BotSettings>>().Value);
                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<NetworkSettings>>().Value);
                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlayerSettings>>().Value);
                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProjectileSettings>>().Value);
                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<SimulationSettings>>().Value);

                    // Configure logging
                    services.Configure<LoggingSettings>(hostContext.Configuration.GetSection("Logging"));
                    services.AddSingleton(s => s.GetRequiredService<Microsoft.Extensions.Options.IOptions<LoggingSettings>>().Value);
                    services.AddSingleton<Shared.Logging.ILogger, ConsoleLogger>();

                    // Register all the server-only services
                    services.RegisterServerTypes();

                    services.AddHostedService<Main>();
                })
                .ConfigureLogging((_, configLogging) => { configLogging.AddConsole(); })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}