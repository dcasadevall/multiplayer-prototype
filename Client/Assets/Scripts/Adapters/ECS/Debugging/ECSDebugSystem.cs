using System.Collections.Generic;
using System.Linq;
using Shared.ECS;
using Shared.ECS.Entities;
using Shared.Logging;

namespace Adapters.ECS.Debugging
{
    /// <summary>
    /// ECS system that provides debugging information about entity and component changes.
    /// Logs entity creation, destruction, and component modifications.
    /// </summary>
    public class EcsDebugSystem : ISystem
    {
        private readonly ILogger _logger;
        private readonly HashSet<EntityId> _knownEntities = new();
        private readonly Dictionary<EntityId, HashSet<System.Type>> _entityComponents = new();
        
        private bool _isInitialized = false;
        private uint _lastLogTick = 0;
        private const uint LOG_INTERVAL = 60; // Log every 60 ticks (about 2 seconds at 30Hz)
        
        public EcsDebugSystem(ILogger logger)
        {
            _logger = logger;
        }
        
        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            if (!_isInitialized)
            {
                Initialize(registry);
                _isInitialized = true;
            }
            
            // Log periodic summary
            if (tickNumber - _lastLogTick >= LOG_INTERVAL)
            {
                LogPeriodicSummary(registry, tickNumber);
                _lastLogTick = tickNumber;
            }
            
            // Check for entity changes
            CheckEntityChanges(registry);
        }
        
        private void Initialize(EntityRegistry registry)
        {
            var entities = registry.GetAll().ToList();
            foreach (var entity in entities)
            {
                _knownEntities.Add(entity.Id);
                _entityComponents[entity.Id] = new HashSet<System.Type>(
                    entity.GetAllComponents().Select(c => c.GetType())
                );
            }
            
            _logger.Info($"ECS Debug System initialized with {entities.Count} existing entities");
        }
        
        private void CheckEntityChanges(EntityRegistry registry)
        {
            var currentEntities = registry.GetAll().ToList();
            var currentEntityIds = currentEntities.Select(e => e.Id).ToHashSet();
            
            // Check for new entities
            foreach (var entity in currentEntities)
            {
                if (!_knownEntities.Contains(entity.Id))
                {
                    LogEntityCreated(entity);
                    _knownEntities.Add(entity.Id);
                    _entityComponents[entity.Id] = new HashSet<System.Type>();
                }
                
                // Check for component changes
                CheckComponentChanges(entity);
            }
            
            // Check for destroyed entities
            var destroyedEntities = _knownEntities.Except(currentEntityIds).ToList();
            foreach (var entityId in destroyedEntities)
            {
                LogEntityDestroyed(entityId);
                _knownEntities.Remove(entityId);
                _entityComponents.Remove(entityId);
            }
        }
        
        private void CheckComponentChanges(Entity entity)
        {
            var currentComponents = entity.GetAllComponents().Select(c => c.GetType()).ToHashSet();
            var previousComponents = _entityComponents[entity.Id];
            
            // Check for added components
            var addedComponents = currentComponents.Except(previousComponents);
            foreach (var componentType in addedComponents)
            {
                LogComponentAdded(entity, componentType);
            }
            
            // Check for removed components
            var removedComponents = previousComponents.Except(currentComponents);
            foreach (var componentType in removedComponents)
            {
                LogComponentRemoved(entity, componentType);
            }
            
            // Update our tracking
            _entityComponents[entity.Id] = currentComponents;
        }
        
        private void LogEntityCreated(Entity entity)
        {
            var componentNames = string.Join(", ", entity.GetAllComponents().Select(c => c.GetType().Name));
            _logger.Info($"Entity created: {entity.Id} with components: [{componentNames}]");
        }
        
        private void LogEntityDestroyed(EntityId entityId)
        {
            _logger.Info($"Entity destroyed: {entityId}");
        }
        
        private void LogComponentAdded(Entity entity, System.Type componentType)
        {
            _logger.Info($"Component added to {entity.Id}: {componentType.Name}");
        }
        
        private void LogComponentRemoved(Entity entity, System.Type componentType)
        {
            _logger.Info($"Component removed from {entity.Id}: {componentType.Name}");
        }
        
        private void LogPeriodicSummary(EntityRegistry registry, uint tickNumber)
        {
            var entities = registry.GetAll().ToList();
            var componentBreakdown = GetComponentBreakdown(entities);
            
            _logger.Info($"ECS Summary (Tick {tickNumber}): {entities.Count} entities");
            
            foreach (var kvp in componentBreakdown.OrderByDescending(x => x.Value))
            {
                _logger.Info($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        private Dictionary<string, int> GetComponentBreakdown(List<Entity> entities)
        {
            var breakdown = new Dictionary<string, int>();
            
            foreach (var entity in entities)
            {
                foreach (var component in entity.GetAllComponents())
                {
                    var componentName = component.GetType().Name;
                    breakdown[componentName] = breakdown.GetValueOrDefault(componentName, 0) + 1;
                }
            }
            
            return breakdown;
        }
    }
} 