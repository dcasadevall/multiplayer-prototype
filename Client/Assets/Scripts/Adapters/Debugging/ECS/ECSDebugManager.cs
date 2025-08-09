#if UNITY_EDITOR
using Adapters.Debugging.ECS.Editor;
#endif

using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Adapters.Debugging.ECS
{
    /// <summary>
    /// Manager component that enables ECS debugging features.
    /// Add this to a GameObject in your scene to enable debugging.
    /// </summary>
    public class EcsDebugManager : MonoBehaviour
    {
        [Header("Debug Components")]
        [SerializeField] private bool _enableVisualDebugger = true;
        
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
                Destroy(_visualDebugger);
            }
        }
        
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Debug/ECS/Add Debug Manager to Scene")]
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
            UnityEditor.Selection.activeGameObject = debugObject;
            Debug.Log("ECS Debug Manager added to scene. Make sure to register it with your DI container.");
        }
        
        [UnityEditor.MenuItem("Debug/ECS/Open ECS Inspector")]
        private static void OpenEcsInspector()
        {
            EcsInspectorWindow.ShowWindow();
        }
#endif
    }
}