using Microsoft.Extensions.DependencyInjection;

namespace Shared.Scheduling
{
    /// <summary>
    /// Extension methods for registering and configuring scheduling services.
    /// </summary>
    public static class SchedulingExtensions
    {
        /// <summary>
        /// Registers <see cref="TimerScheduler"/> as the default <see cref="IScheduler"/> in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register with.</param>
        /// <returns>The updated service collection.</returns>
        public static void RegisterSchedulingTypes(this IServiceCollection services)
        { 
            services.AddSingleton<IScheduler, TimerScheduler>();
        }
    }
}