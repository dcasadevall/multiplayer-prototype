using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Scheduling;
using UnityEngine;
using System.Linq;
using Shared;

namespace Core
{
    /// <summary>
    /// Manages the client-side ECS world and coordinates networking with the server.
    /// 
    /// <para>
    /// This class is responsible for:
    /// - Creating and managing the local ECS world
    /// - Setting up the replication system to receive server snapshots
    /// - Managing the lifecycle of the client world
    /// </para>
    /// </summary>
    public class ClientWorldManager : MonoBehaviour
    {
        [SerializeField] 
        private UnityServiceProvider _serviceProvider;
        
        private World _world;
        
        private void Awake()
        {
            Debug.Log("ClientWorldManager: Initializing ECS world with DI...");
            
            // Get services from DI container
            var entityRegistry = _serviceProvider.GetService<EntityRegistry>();
            var scheduler = _serviceProvider.GetService<IScheduler>();
            
            // Create a world using the WorldBuilder pattern (like the server)
            var worldBuilder = new WorldBuilder(entityRegistry, scheduler)
                .WithFrequency(SharedConstants.WorldTickRate);
            
            // Add all registered systems to the world
            var systems = _serviceProvider.GetServices<ISystem>().ToList();
            systems.ForEach(system => worldBuilder.AddSystem(system));
            
            // Build the world
            _world = worldBuilder.Build();
            
            Debug.Log($"ClientWorldManager: ECS world initialized with {SharedConstants.WorldTickRate} ticks per second and {systems.Count} systems");
        }
        
        /// <summary>
        /// Starts the ECS world, similar to how a server would start its world.
        /// </summary>
        private void Start()
        {
            Debug.Log("Starting ECS world...");
            _world.Start();
            Debug.Log("ECS world started successfully");
        }

        /// <summary>
        /// Destroys the ECS world and cleans up resources.
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("ClientWorldManager: Cleaning up world...");
            _world?.Dispose();
            Debug.Log("ClientWorldManager: World cleanup completed");
        }
    }
} 