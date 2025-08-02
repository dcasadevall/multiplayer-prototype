using System;
using System.Collections.Generic;
using System.Linq;
using Core.ECS.Rendering;
using Core.Logging;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.ECS;
using Shared.Scheduling;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Core
{
    /// <summary>
    /// Unity-compatible service provider that manages dependency injection for the client.
    /// The game / adapter should use the <see cref="UnityServiceProvider"/> to initialize
    /// the core services, and then add any game-specific services.
    /// 
    /// <para>
    /// This class provides a bridge between Unity's MonoBehaviour lifecycle and the
    /// dependency injection pattern used by the server. It manages the creation and
    /// disposal of services, ensuring proper resource management.
    /// </para>
    /// </summary>
    public class UnityServiceProvider : MonoBehaviour
    {
        private IServiceProvider _serviceProvider;
        
        [SerializeField]
        private UnityMainThreadScheduler _mainThreadScheduler;
        
        /// <summary>
        /// Adds the core services to the provided service collection and builds the service provider.
        /// This method should be called during the initialization phase of the application,
        /// passing in the game specific service collection.
        /// </summary>
        public void RegisterCoreServices(IServiceCollection services)
        {
            Debug.Log("CoreServiceProvider: Initializing core dependency injection...");

            // Register ECS core
            services.AddSingleton<EntityRegistry>();
            
            // Logging
            services.AddSingleton<ILogger, UnityLogger>();
            
            // Register client systems
            services.AddSingleton<ISystem, ClientReplicationSystem>();
            services.AddSingleton<ISystem, EntityViewSystem>();
            services.AddSingleton<IEntityViewRegistry>(sp => sp.GetService<EntityViewSystem>());
            services.AddSingleton<IDisposable>(sp => sp.GetService<EntityViewSystem>());
            
            // Register scheduler and lifecycle management
            // The IScheduler implementation is client-specific
            services.AddSingleton<IScheduler>(_mainThreadScheduler);
            
            // Registers Json serialization, NetLib and shared scheduling types
            services.RegisterSharedTypes();
            
            _serviceProvider = services.BuildServiceProvider();
            
            Debug.Log("UnityServiceProvider: Core Dependency injection initialized successfully");
        }

        /// <summary>
        /// Start is used to initialize all services that implement <see cref="IInitializable"/>.
        /// </summary>
        private void Start()
        {
            var initializables = _serviceProvider.GetServices<IInitializable>();
            initializables.ToList().ForEach(x => x.Initialize()); 
        }
        
        /// <summary>
        /// Disposes of the service provider and cleans up resources.
        /// Calls Dispose on all registered IDisposable services.
        /// </summary>
        private void OnDestroy()
        {
            var disposables = _serviceProvider.GetServices<IDisposable>();
            disposables.ToList().ForEach(x => x.Dispose());
            
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _serviceProvider = null;
            Debug.Log("UnityServiceProvider: Disposed successfully");
        }
                
        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetService<T>() where T : class
        {
            return _serviceProvider?.GetRequiredService<T>();
        }
        
        /// <summary>
        /// Gets all services of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of services to retrieve.</typeparam>
        /// <returns>All service instances of the specified type.</returns>
        public IEnumerable<T> GetServices<T>() where T : class
        {
            return _serviceProvider?.GetServices<T>();
        }
    }
} 