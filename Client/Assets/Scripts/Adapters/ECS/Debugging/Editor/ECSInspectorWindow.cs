#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Entities;
using UnityEditor;
using UnityEngine;

namespace Adapters.ECS.Debugging
{
    /// <summary>
    /// Unity Editor window for inspecting ECS entities and components in real-time.
    /// </summary>
    public class EcsInspectorWindow : EditorWindow
    {
        private EntityRegistry _entityRegistry;
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private float _refreshInterval = 0.5f;
        private DateTime _lastRefreshTime;
        private string _searchFilter = "";
        private bool _showOnlyWithPosition = false;
        private bool _showOnlyPlayers = false;
        private bool _showOnlyProjectiles = false;
        
        private readonly List<EntityInfo> _entityInfos = new();
        
        [MenuItem("Window/ECS Inspector")]
        public static void ShowWindow()
        {
            GetWindow<EcsInspectorWindow>("ECS Inspector");
        }
        
        private void OnEnable()
        {
            // Try to find the entity registry from the scene
            FindEntityRegistry();
        }
        
        private void Update()
        {
            if (_autoRefresh && (DateTime.Now - _lastRefreshTime).TotalSeconds >= _refreshInterval)
            {
                RefreshEntityList();
                _lastRefreshTime = DateTime.Now;
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawFilters();
            DrawEntityList();
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshEntityList();
            }
            
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            if (_entityRegistry != null)
            {
                var entityCount = _entityRegistry.GetAll().Count();
                GUILayout.Label($"Entities: {entityCount}", EditorStyles.toolbarButton);
            }
            else
            {
                GUILayout.Label("No ECS World Found", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            
            _searchFilter = EditorGUILayout.TextField("Search", _searchFilter);
            
            EditorGUILayout.BeginHorizontal();
            _showOnlyWithPosition = GUILayout.Toggle(_showOnlyWithPosition, "With Position");
            _showOnlyPlayers = GUILayout.Toggle(_showOnlyPlayers, "Players Only");
            _showOnlyProjectiles = GUILayout.Toggle(_showOnlyProjectiles, "Projectiles Only");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEntityList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            if (_entityInfos.Count == 0)
            {
                EditorGUILayout.HelpBox("No entities found or no ECS world available.", MessageType.Info);
            }
            else
            {
                foreach (var entityInfo in _entityInfos)
                {
                    DrawEntityInfo(entityInfo);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawEntityInfo(EntityInfo entityInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Entity header
            EditorGUILayout.BeginHorizontal();
            var isExpanded = EditorGUILayout.Foldout(entityInfo.IsExpanded, $"Entity {entityInfo.Id}", true);
            entityInfo.IsExpanded = isExpanded;
            
            // Component count
            GUILayout.Label($"({entityInfo.ComponentCount} components)", EditorStyles.miniLabel);
            
            // Position info if available
            if (entityInfo.HasPosition)
            {
                GUILayout.Label($"Pos: ({entityInfo.Position.x:F1}, {entityInfo.Position.y:F1}, {entityInfo.Position.z:F1})", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Entity details (when expanded)
            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Component list
                foreach (var component in entityInfo.Components)
                {
                    DrawComponentInfo(component);
                }
                
                // Actions
                EditorGUILayout.Space();
                DrawEntityActions(entityInfo);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawComponentInfo(ComponentInfo componentInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(componentInfo.TypeName, EditorStyles.boldLabel);
            
            // Component properties
            foreach (var property in componentInfo.Properties)
            {
                EditorGUILayout.LabelField(property.Key, property.Value);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEntityActions(EntityInfo entityInfo)
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Focus in Scene"))
            {
                FocusEntityInScene(entityInfo);
            }
            
            if (GUILayout.Button("Log Details"))
            {
                LogEntityDetails(entityInfo);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void FindEntityRegistry()
        {
            // Try to find the entity registry from the scene
#pragma warning disable CS0618 // Type or member is obsolete
            var serviceProvider = FindObjectOfType<RootServiceProvider>();
#pragma warning restore CS0618 // Type or member is obsolete
            _entityRegistry = serviceProvider?.ServiceProvider?.GetRequiredService<EntityRegistry>();
        }
        
        private void RefreshEntityList()
        {
            if (_entityRegistry == null)
            {
                FindEntityRegistry();
                if (_entityRegistry == null)
                {
                    _entityInfos.Clear();
                    return;
                }
            }
            
            // Preserve expanded states
            var expandedStates = new Dictionary<string, bool>();
            foreach (var entityInfo in _entityInfos)
            {
                expandedStates[entityInfo.Id] = entityInfo.IsExpanded;
            }
            
            _entityInfos.Clear();
            var entities = _entityRegistry.GetAll().ToList();
            
            foreach (var entity in entities)
            {
                var entityInfo = CreateEntityInfo(entity);
                
                // Apply filters
                if (!PassesFilters(entityInfo))
                    continue;
                
                // Restore expanded state if it existed before
                if (expandedStates.TryGetValue(entityInfo.Id, out var wasExpanded))
                {
                    entityInfo.IsExpanded = wasExpanded;
                }
                
                _entityInfos.Add(entityInfo);
            }
        }
        
        private EntityInfo CreateEntityInfo(Entity entity)
        {
            var components = entity.GetAllComponents().ToList();
            var componentInfos = new List<ComponentInfo>();
            
            foreach (var component in components)
            {
                var componentInfo = CreateComponentInfo(component);
                componentInfos.Add(componentInfo);
            }
            
            Vector3 position = Vector3.zero;
            bool hasPosition = entity.TryGet<PositionComponent>(out var posComponent);
            if (hasPosition)
            {
                position = new Vector3(posComponent.X, posComponent.Y, posComponent.Z);
            }
            
            return new EntityInfo
            {
                Id = entity.Id.ToString(),
                ComponentCount = components.Count,
                Components = componentInfos,
                Position = position,
                HasPosition = hasPosition,
                IsExpanded = false
            };
        }
        
        private ComponentInfo CreateComponentInfo(IComponent component)
        {
            var componentType = component.GetType();
            var properties = new Dictionary<string, string>();
            
            // Extract component properties using reflection
            var componentProperties = componentType.GetProperties();
            foreach (var property in componentProperties)
            {
                try
                {
                    var value = property.GetValue(component);
                    
                    // Handle nested components (like PredictedComponent<T>)
                    if (value is IComponent nestedComponent)
                    {
                        properties[property.Name] = "Nested Component";
                        
                        // Add nested component properties with indentation
                        var nestedProperties = GetComponentProperties(nestedComponent);
                        foreach (var nestedProp in nestedProperties)
                        {
                            properties[$"  {nestedProp.Key}"] = nestedProp.Value;
                        }
                    }
                    else
                    {
                        properties[property.Name] = value?.ToString() ?? "null";
                    }
                }
                catch
                {
                    properties[property.Name] = "Error reading value";
                }
            }
            
            return new ComponentInfo
            {
                TypeName = GetComponentDisplayName(componentType),
                Properties = properties
            };
        }
        
        private Dictionary<string, string> GetComponentProperties(IComponent component)
        {
            var properties = new Dictionary<string, string>();
            var componentType = component.GetType();
            
            var componentProperties = componentType.GetProperties();
            foreach (var property in componentProperties)
            {
                try
                {
                    var value = property.GetValue(component);
                    
                    // Handle deeply nested components
                    if (value is IComponent deeplyNestedComponent)
                    {
                        properties[property.Name] = "Nested Component";
                        
                        var deeplyNestedProperties = GetComponentProperties(deeplyNestedComponent);
                        foreach (var deeplyNestedProp in deeplyNestedProperties)
                        {
                            properties[$"  {deeplyNestedProp.Key}"] = deeplyNestedProp.Value;
                        }
                    }
                    else
                    {
                        properties[property.Name] = value?.ToString() ?? "null";
                    }
                }
                catch
                {
                    properties[property.Name] = "Error reading value";
                }
            }
            
            return properties;
        }
        
        private string GetComponentDisplayName(Type componentType)
        {
            // Handle generic types like PredictedComponent<T>
            if (componentType.IsGenericType)
            {
                var genericArguments = componentType.GetGenericArguments();
                var typeNames = genericArguments.Select(t => t.Name).ToArray();
                var baseName = componentType.Name.Substring(0, componentType.Name.IndexOf('`'));
                return $"{baseName}<{string.Join(", ", typeNames)}>";
            }
            
            return componentType.Name;
        }
        
        private bool PassesFilters(EntityInfo entityInfo)
        {
            // Search filter
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                var searchLower = _searchFilter.ToLower();
                var matchesSearch = entityInfo.Id.ToLower().Contains(searchLower) ||
                                   entityInfo.Components.Any(c => c.TypeName.ToLower().Contains(searchLower));
                
                if (!matchesSearch)
                    return false;
            }
            
            // Position filter
            if (_showOnlyWithPosition && !entityInfo.HasPosition)
                return false;
            
            // Player filter
            if (_showOnlyPlayers)
            {
                var hasPlayerComponent = entityInfo.Components.Any(c => c.TypeName.Contains("Player"));
                if (!hasPlayerComponent)
                    return false;
            }
            
            // Projectile filter
            if (_showOnlyProjectiles)
            {
                var hasProjectileComponent = entityInfo.Components.Any(c => c.TypeName.Contains("Projectile"));
                if (!hasProjectileComponent)
                    return false;
            }
            
            return true;
        }
        
        private void FocusEntityInScene(EntityInfo entityInfo)
        {
            if (entityInfo.HasPosition)
            {
                SceneView.FrameLastActiveSceneView();
                SceneView.lastActiveSceneView?.Frame(new Bounds(entityInfo.Position, Vector3.one), false);
            }
        }
        
        private void LogEntityDetails(EntityInfo entityInfo)
        {
            var log = $"Entity {entityInfo.Id}:\n";
            log += $"  Position: {entityInfo.Position}\n";
            log += $"  Components ({entityInfo.ComponentCount}):\n";
            
            foreach (var component in entityInfo.Components)
            {
                log += $"    {component.TypeName}:\n";
                foreach (var property in component.Properties)
                {
                    log += $"      {property.Key}: {property.Value}\n";
                }
            }
            
            Debug.Log(log);
        }
        
        [System.Serializable]
        private class EntityInfo
        {
            public string Id = string.Empty;
            public int ComponentCount;
            public List<ComponentInfo> Components = new();
            public Vector3 Position;
            public bool HasPosition;
            public bool IsExpanded;
        }
        
        [System.Serializable]
        private class ComponentInfo
        {
            public string TypeName = string.Empty;
            public Dictionary<string, string> Properties = new();
        }
    }
} 
#endif
