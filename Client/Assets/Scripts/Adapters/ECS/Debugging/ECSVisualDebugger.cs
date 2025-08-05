using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using UnityEngine;
using ILogger = Shared.Logging.ILogger;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Adapters.ECS.Debugging
{
    /// <summary>
    /// Provides visual debugging information in the Unity Scene view.
    /// Shows entity positions, components, and relationships.
    /// </summary>
    public class ECSVisualDebugger : MonoBehaviour
    {
        [Header("Visual Debug Settings")]
        [SerializeField] private bool _enableVisualDebugging = true;
        [SerializeField] private bool _showEntityPositions = true;
        [SerializeField] private bool _showEntityLabels = true;
        [SerializeField] private bool _showComponentInfo = true;
        
        [Header("Display Options")]
        [SerializeField] private float _labelOffset = 1f;
        [SerializeField] private float _sphereRadius = 0.5f;
        [SerializeField] private int _maxLabelsToShow = 50;
        [SerializeField] private float _labelUpdateInterval = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color _playerColor = Color.blue;
        [SerializeField] private Color _projectileColor = Color.red;
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _labelColor = Color.yellow;
        
        private IServiceProvider _serviceProvider;
        private EntityRegistry _entityRegistry;
        private float _lastLabelUpdate;
        
        // Cached entity data for performance
        private readonly List<EntityVisualInfo> _entityVisualInfos = new();
        
        private void Awake()
        {
            _serviceProvider = FindAnyObjectByType<RootServiceProvider>()?.ServiceProvider;
            Debug.Log("ECS Visual Debugger initialized");
        }
        
        private void Update()
        {
            if (!_enableVisualDebugging)
            {
                return;
            }

            if (_entityRegistry == null)
            {
                _entityRegistry = _serviceProvider.GetRequiredService<EntityRegistry>();
            }
                
            if (Time.time - _lastLabelUpdate >= _labelUpdateInterval)
            {
                UpdateEntityVisualInfo();
                _lastLabelUpdate = Time.time;
            }
        }
        
        private void UpdateEntityVisualInfo()
        {
            _entityVisualInfos.Clear();
            
            var entities = _entityRegistry.GetAll().Take(_maxLabelsToShow).ToList();
            
            foreach (var entity in entities)
            {
                var visualInfo = new EntityVisualInfo
                {
                    EntityId = entity.Id.ToString(),
                    Position = GetEntityPosition(entity),
                    Color = GetEntityColor(entity),
                    Components = entity.GetAllComponents()
                        .Select(c => c.GetType().Name)
                        .ToArray(),
                    HasPosition = entity.Has<PositionComponent>()
                };
                
                _entityVisualInfos.Add(visualInfo);
            }
        }
        
        private Vector3 GetEntityPosition(Entity entity)
        {
            if (entity.TryGet<PositionComponent>(out var position))
            {
                return new Vector3(position.X, position.Y, position.Z);
            }
            
            return Vector3.zero;
        }
        
        private Color GetEntityColor(Entity entity)
        {
            // Determine color based on entity type/components
            if (entity.Has<PlayerTagComponent>())
                return _playerColor;
            else if (entity.Has<ProjectileTagComponent>())
                return _projectileColor;
            else
                return _defaultColor;
        }
        
        private void OnDrawGizmos()
        {
            if (!_enableVisualDebugging || _entityRegistry == null)
                return;
                
            DrawEntityGizmos();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_enableVisualDebugging || _entityRegistry == null)
                return;
                
            DrawSelectedEntityGizmos();
        }
        
        private void DrawEntityGizmos()
        {
            foreach (var info in _entityVisualInfos)
            {
                if (!info.HasPosition)
                    continue;
                    
                // Draw entity sphere
                Gizmos.color = info.Color;
                Gizmos.DrawWireSphere(info.Position, _sphereRadius);
                
                // Draw entity label
                if (_showEntityLabels)
                {
                    DrawEntityLabel(info);
                }
            }
        }
        
        private void DrawSelectedEntityGizmos()
        {
            // Draw additional info for selected entities
            foreach (var info in _entityVisualInfos)
            {
                if (!info.HasPosition)
                    continue;
                    
                // Draw component info
                if (_showComponentInfo)
                {
                    DrawComponentInfo(info);
                }
            }
        }
        
        private void DrawEntityLabel(EntityVisualInfo info)
        {
            var labelPosition = info.Position + Vector3.up * _labelOffset;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = _labelColor;
            UnityEditor.Handles.Label(labelPosition, $"Entity {info.EntityId}");
            #endif
        }
        
        private void DrawComponentInfo(EntityVisualInfo info)
        {
            var componentText = string.Join("\n", info.Components);
            var labelPosition = info.Position + Vector3.up * (_labelOffset + 0.5f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = _labelColor;
            UnityEditor.Handles.Label(labelPosition, componentText);
            #endif
        }
        
        #region Unity Editor Integration
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("Debug/ECS/Show Entity Positions")]
        private static void ToggleEntityPositions()
        {
            var debugger = FindObjectOfType<ECSVisualDebugger>();
            if (debugger != null)
            {
                debugger._showEntityPositions = !debugger._showEntityPositions;
                UnityEditor.EditorUtility.SetDirty(debugger);
            }
        }
        
        [UnityEditor.MenuItem("Debug/ECS/Show Entity Labels")]
        private static void ToggleEntityLabels()
        {
            var debugger = FindObjectOfType<ECSVisualDebugger>();
            if (debugger != null)
            {
                debugger._showEntityLabels = !debugger._showEntityLabels;
                UnityEditor.EditorUtility.SetDirty(debugger);
            }
        }
        
        [UnityEditor.MenuItem("Debug/ECS/Log Entity Positions")]
        private static void LogEntityPositions()
        {
            var debugger = FindObjectOfType<ECSVisualDebugger>();
            if (debugger?._entityRegistry != null)
            {
                var entities = debugger._entityRegistry.GetAll().ToList();
                Debug.Log($"Found {entities.Count} entities:");
                
                foreach (var entity in entities)
                {
                    if (entity.TryGet<PositionComponent>(out var pos))
                    {
                        Debug.Log($"Entity {entity.Id}: Position ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
                    }
                    else
                    {
                        Debug.Log($"Entity {entity.Id}: No position component");
                    }
                }
            }
        }
        #endif
        
        #endregion
        
        [System.Serializable]
        private class EntityVisualInfo
        {
            public string EntityId = string.Empty;
            public Vector3 Position;
            public Color Color;
            public string[] Components = System.Array.Empty<string>();
            public bool HasPosition;
        }
    }
} 