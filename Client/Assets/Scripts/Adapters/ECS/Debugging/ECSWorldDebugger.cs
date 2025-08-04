using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.ECS.Simulation;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;

namespace Adapters.ECS.Debugging
{
    /// <summary>
    /// Provides comprehensive debugging information about the ECS world in Unity.
    /// Shows entities, components, and system information in real-time.
    /// </summary>
    public class ECSWorldDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugging = true;
        [SerializeField] private bool _logToConsole = true;
        [SerializeField] private bool _showInInspector = true;
        [SerializeField] private float _updateInterval = 1f; // How often to update debug info
        
        [Header("Display Options")]
        [SerializeField] private bool _showEntityCount = true;
        [SerializeField] private bool _showComponentBreakdown = true;
        [SerializeField] private bool _showSystemInfo = true;
        [SerializeField] private bool _showEntityDetails = true;
        [SerializeField] private int _maxEntitiesToShow = 20;
        
        [Header("Debug Information")]
        [SerializeField] private string _worldInfo = "No world available";
        [SerializeField] private string _entityCount = "0";
        [SerializeField] private string _componentBreakdown = "No components";
        [SerializeField] private string _systemInfo = "No systems";
        [SerializeField] private string _entityDetails = "No entities";
        
        private World? _world;
        private EntityRegistry? _entityRegistry;
        private ILogger? _logger;
        private IServiceProvider _serviceProvider;
        
        private float _lastUpdateTime;
        
        // Runtime debug data
        private readonly Dictionary<Type, int> _componentCounts = new();
        private readonly List<EntityDebugInfo> _entityDebugInfos = new();
        
        private void Awake()
        {
            _serviceProvider = FindAnyObjectByType<RootServiceProvider>()?.ServiceProvider;
            if (_enableDebugging)
            {
                Debug.Log("ECS World Debugger initialized");
            }
        }
        
        private void Update()
        {
            if (!_enableDebugging)
                return;

            if (_entityRegistry == null)
            {
                _entityRegistry = _serviceProvider.GetRequiredService<EntityRegistry>();
                _logger = _serviceProvider.GetRequiredService<ILogger>();
            }
                
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                UpdateDebugInformation();
                _lastUpdateTime = Time.time;
            }
        }
        
        private void UpdateDebugInformation()
        {
            try
            {
                UpdateWorldInfo();
                UpdateEntityCount();
                UpdateComponentBreakdown();
                UpdateSystemInfo();
                UpdateEntityDetails();
                
                if (_logToConsole)
                {
                    LogDebugInfo();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating ECS debug info: {e}");
            }
        }
        
        private void UpdateWorldInfo()
        {
            if (_world == null) return;
            
            _worldInfo = $"Tick: {_world.CurrentTickIndex} | " +
                        $"Delta: {_world.FixedDeltaTime:F3}s | " +
                        $"Rate: {_world.TickRate.TotalMilliseconds:F1}ms";
        }
        
        private void UpdateEntityCount()
        {
            if (_entityRegistry == null) return;
            
            var entities = _entityRegistry.GetAll().ToList();
            _entityCount = entities.Count.ToString();
        }
        
        private void UpdateComponentBreakdown()
        {
            if (_entityRegistry == null) return;
            
            _componentCounts.Clear();
            var entities = _entityRegistry.GetAll().ToList();
            
            foreach (var entity in entities)
            {
                foreach (var component in entity.GetAllComponents())
                {
                    var componentType = component.GetType();
                    _componentCounts[componentType] = _componentCounts.GetValueOrDefault(componentType, 0) + 1;
                }
            }
            
            var breakdown = new StringBuilder();
            foreach (var kvp in _componentCounts.OrderByDescending(x => x.Value))
            {
                breakdown.AppendLine($"{kvp.Key.Name}: {kvp.Value}");
            }
            
            _componentBreakdown = breakdown.Length > 0 ? breakdown.ToString() : "No components";
        }
        
        private void UpdateSystemInfo()
        {
            // Note: We don't have direct access to systems from World, but we can show what we know
            _systemInfo = $"World Running: {_world?.CurrentTickIndex > 0}";
        }
        
        private void UpdateEntityDetails()
        {
            if (_entityRegistry == null) return;
            
            _entityDebugInfos.Clear();
            var entities = _entityRegistry.GetAll().Take(_maxEntitiesToShow).ToList();
            
            foreach (var entity in entities)
            {
                var debugInfo = new EntityDebugInfo
                {
                    Id = entity.Id.ToString(),
                    ComponentCount = entity.GetAllComponents().Count(),
                    Components = entity.GetAllComponents()
                        .Select(c => c.GetType().Name)
                        .ToArray()
                };
                
                _entityDebugInfos.Add(debugInfo);
            }
            
            var details = new StringBuilder();
            foreach (var info in _entityDebugInfos)
            {
                details.AppendLine($"Entity {info.Id} ({info.ComponentCount} components): {string.Join(", ", info.Components)}");
            }
            
            _entityDetails = details.Length > 0 ? details.ToString() : "No entities";
        }
        
        private void LogDebugInfo()
        {
            if (_logger == null) return;
            
            _logger.Info($"ECS Debug - {_worldInfo} | Entities: {_entityCount}");
            
            if (_componentBreakdown != "No components")
            {
                _logger.Info($"Component Breakdown:\n{_componentBreakdown}");
            }
        }
        
        [System.Serializable]
        private class EntityDebugInfo
        {
            public string Id = string.Empty;
            public int ComponentCount;
            public string[] Components = Array.Empty<string>();
        }
        
        #region Unity Inspector Debugging
        
        [ContextMenu("Dump Full World State")]
        private void DumpFullWorldState()
        {
            if (_entityRegistry == null) return;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== ECS WORLD DUMP ===");
            sb.AppendLine($"World Info: {_worldInfo}");
            sb.AppendLine($"Entity Count: {_entityCount}");
            sb.AppendLine();
            
            sb.AppendLine("=== COMPONENT BREAKDOWN ===");
            sb.AppendLine(_componentBreakdown);
            sb.AppendLine();
            
            sb.AppendLine("=== ENTITY DETAILS ===");
            sb.AppendLine(_entityDetails);
            
            Debug.Log(sb.ToString());
        }
        
        [ContextMenu("Log All Entities")]
        private void LogAllEntities()
        {
            if (_entityRegistry == null) return;
            
            var entities = _entityRegistry.GetAll().ToList();
            Debug.Log($"Found {entities.Count} entities in the world");
            
            foreach (var entity in entities)
            {
                var components = entity.GetAllComponents().Select(c => c.GetType().Name).ToArray();
                Debug.Log($"Entity {entity.Id}: {string.Join(", ", components)}");
            }
        }
        
        [ContextMenu("Log Component Counts")]
        private void LogComponentCounts()
        {
            Debug.Log($"Component Breakdown:\n{_componentBreakdown}");
        }
        
        #endregion
    }
} 