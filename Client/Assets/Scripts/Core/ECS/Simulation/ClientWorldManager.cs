using System;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.TickSync;
using Shared.Scheduling;
using UnityEngine;

namespace Core.ECS.Simulation
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
    public class ClientWorldManager : IInitializable, IDisposable
    {
        private readonly EntityRegistry _entityRegistry;
        private readonly ITickSync _tickSync;
        private readonly IScheduler _scheduler;
        private readonly IEnumerable<ISystem> _systems;
        private World _world;

        public ClientWorldManager(EntityRegistry entityRegistry, 
            ITickSync tickSync,
            IScheduler scheduler, 
            IEnumerable<ISystem> systems)
        {
            _entityRegistry = entityRegistry;
            _tickSync = tickSync;
            _scheduler = scheduler;
            _systems = systems;
        }

        public void Initialize()
        {
            Debug.Log("ClientWorldManager: Initializing ECS world with DI...");
            
            // Create a world using the WorldBuilder pattern (like the server)
            var worldBuilder = new WorldBuilder(_entityRegistry, _tickSync, _scheduler)
                .WithFrequency(SharedConstants.WorldTicksPerSecond)
                .WithWorldMode(WorldMode.Client);
            
            // Add all registered systems to the world
            _systems.ToList().ForEach(system => worldBuilder.AddSystem(system));
            
            // Build the world
            _world = worldBuilder.Build();
            _world.Start();
            
            Debug.Log($"ClientWorldManager: ECS world initialized with {SharedConstants.WorldTicksPerSecond} " +
                      $"ticks per second and {_systems.Count()} systems");
        }
        
        /// <summary>
        /// Disposes the ECS world and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Debug.Log("ClientWorldManager: Cleaning up world...");
            _world?.Dispose();
            Debug.Log("ClientWorldManager: World cleanup completed");
        }
    }
} 