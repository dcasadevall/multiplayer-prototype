using System;
using System.Collections.Generic;
using Core.Scheduling;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using Shared.ECS;
using Shared.Networking;
using Shared.Networking.Replication;
using Shared.Scheduling;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Core
{
    /// <summary>
    /// Unity-compatible service provider that manages dependency injection for the client.
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
        private IServiceCollection _services;
        
        [SerializeField]
        private UnityMessageReceiver _messageReceiver;
        
        [SerializeField]
        private UnityMainThreadScheduler _mainThreadScheduler;
        
        /// <summary>
        /// Initializes the service collection and builds the service provider.
        /// </summary>
        public void Initialize()
        {
            Debug.Log("UnityServiceProvider: Initializing dependency injection...");
            
            _services = new ServiceCollection();
            
            // Register ECS core
            _services.AddSingleton<EntityRegistry>();
            
            // Unity specific services
            _services.AddSingleton<ILogger, UnityLogger>();
            
            // Register Unity-specific services
            _services.AddSingleton<MonoBehaviour>(this);
            
            // Register client systems
            _services.AddSingleton<ISystem, ClientReplicationSystem>();
            _services.AddSingleton<ISystem, EntityViewSystem>();
            
            // Register scheduler to use unity main thread
            _services.AddSingleton<IScheduler>(_mainThreadScheduler);
            
            // Register the son replication types
            _services.RegisterJsonReplicationTypes();
            
            // Register Networking classes
            _services.AddSingleton<IMessageReceiver>(_messageReceiver);
            _services.AddSingleton<EventBasedNetListener>();
            _services.AddSingleton<INetEventListener>(sp => sp.GetRequiredService<EventBasedNetListener>());
            _services.AddSingleton<NetManager>();
            
            // Build the service provider
            _serviceProvider = _services.BuildServiceProvider();
            
            Debug.Log("UnityServiceProvider: Dependency injection initialized successfully");
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
        
        /// <summary>
        /// Disposes of the service provider and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            _serviceProvider = null;
            _services = null;
            
            Debug.Log("UnityServiceProvider: Disposed successfully");
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
    }
} 