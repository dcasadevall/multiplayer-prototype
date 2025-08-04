using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Adapters.ECS.Debugging
{
    /// <summary>
    /// Manager component that enables ECS debugging features.
    /// Add this to a GameObject in your scene to enable debugging.
    /// </summary>
    public class EcsDebugManager : MonoBehaviour
    {
        [Header("Debug Components")]
        [SerializeField] private bool _enableWorldDebugger = true;
        [SerializeField] private bool _enableVisualDebugger = true;
        [SerializeField] private bool _enableDebugSystem = true;
        
        [Header("Settings")]
        [SerializeField] private bool _showDebugInfoInInspector = true;
        [SerializeField] private bool _logToConsole = true;
        
        private ECSVisualDebugger _visualDebugger;
        
        private void Awake()
        {
            if (_enableVisualDebugger)
            {
                _visualDebugger = gameObject.AddComponent<ECSVisualDebugger>();
            }
            
            Debug.Log("ECS Debug Manager initialized. Use Window > ECS Inspector to open the debug window.");
        }
        
        private void OnDestroy()
        {
            if (_visualDebugger != null)
            {
                DestroyImmediate(_visualDebugger);
            }
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("ECS/Debug/Add Debug Manager to Scene")]
        private static void AddDebugManagerToScene()
        {
            var existingManager = FindObjectOfType<EcsDebugManager>();
            if (existingManager != null)
            {
                UnityEditor.Selection.activeGameObject = existingManager.gameObject;
                Debug.LogWarning("ECS Debug Manager already exists in scene. Selected existing instance.");
                return;
            }
            
            var debugObject = new GameObject("ECS Debug Manager");
            var manager = debugObject.AddComponent<EcsDebugManager>();
            
            UnityEditor.Selection.activeGameObject = debugObject;
            Debug.Log("ECS Debug Manager added to scene. Make sure to register it with your DI container.");
        }
        
        [UnityEditor.MenuItem("ECS/Debug/Open ECS Inspector")]
        private static void OpenEcsInspector()
        {
            EcsInspectorWindow.ShowWindow();
        }
#endif
    }
}