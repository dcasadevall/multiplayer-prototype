using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Networking;
using Shared.Scheduling;
using Shared.Networking.Replication;
using UnityEngine;
using System.Linq;

namespace Core
{
    /// <summary>
    /// Manages the client-side ECS world and coordinates networking with the server.
    /// 
    /// <para>
    /// This class is responsible for:
    /// - Creating and managing the local ECS world
    /// - Setting up the replication system to receive server snapshots
    /// - Coordinating between Unity's update loop and the ECS world
    /// - Managing the lifecycle of the client world
    /// </para>
    /// </summary>
    public class ClientWorldManager : MonoBehaviour
    {
        [Header("ECS Configuration")]
        [SerializeField] 
        private int _ticksPerSecond = 30;
        
        [SerializeField] 
        private UnityServiceProvider _serviceProvider;
        
        private World _world;
        
        
        // Unity lifecycle
        private void Awake()
        {
            InitializeWorld();
        }
        
        private void Start()
        {
            StartWorld();
            
            // Start the world (similar to server)
            Debug.Log("Starting fixed timestep world...");
            _world.Start();
        }
        
        private void Update()
        {
            // The world now manages its own ticking through the scheduler
            // No need to manually invoke ticks here
        }
        
        private void OnDestroy()
        {
            CleanupWorld();
        }
        
        /// <summary>
        /// Initializes the ECS world and its systems using dependency injection.
        /// </summary>
        private void InitializeWorld()
        {
            Debug.Log("ClientWorldManager: Initializing ECS world with DI...");
            
            // Initialize the service provider
            _serviceProvider.Initialize();
            
            // Get services from DI container
            var entityRegistry = _serviceProvider.GetService<EntityRegistry>();
            var scheduler = _serviceProvider.GetService<IScheduler>();
            
            // Create a world using the WorldBuilder pattern (like the server)
            var worldBuilder = new WorldBuilder(entityRegistry, scheduler).WithFrequency(_ticksPerSecond);
            
            // Add all registered systems to the world
            var systems = _serviceProvider.GetServices<ISystem>().ToList();
            systems.ForEach(system => worldBuilder.AddSystem(system));
            
            // Build the world
            _world = worldBuilder.Build();
            
            Debug.Log($"ClientWorldManager: ECS world initialized with {_ticksPerSecond} ticks per second and {systems.Count} systems");
        }
        
        /// <summary>
        /// Starts the world and begins processing.
        /// </summary>
        private void StartWorld()
        {
            Debug.Log("ClientWorldManager: Starting world...");
            
            // Start listening for network messages
            var messageReceiver = _serviceProvider.GetService<IMessageReceiver>();
            messageReceiver?.StartListening();
            
            Debug.Log("ClientWorldManager: World started successfully");
        }
        
        /// <summary>
        /// Cleans up the world and its resources.
        /// </summary>
        private void CleanupWorld()
        {
            Debug.Log("ClientWorldManager: Cleaning up world...");
            
            // Stop listening for network messages
            var messageReceiver = _serviceProvider.GetService<IMessageReceiver>();
            messageReceiver?.StopListening();
            
            // Dispose of the world
            _world?.Dispose();
            
            // Dispose of the service provider
            _serviceProvider?.Dispose();
            
            Debug.Log("ClientWorldManager: World cleanup completed");
        }
        
        /// <summary>
        /// Gets the entity registry for external access.
        /// </summary>
        public EntityRegistry EntityRegistry => _serviceProvider?.GetService<EntityRegistry>();
        
        /// <summary>
        /// Gets the world for external access.
        /// </summary>
        public World World => _world;
    }
} 