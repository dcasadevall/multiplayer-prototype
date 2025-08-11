using UnityEngine;

namespace Adapters.Rendering
{
    /// <summary>
    /// This component allows you to set the color of a MeshRenderer's material.
    /// It is useful for dynamically changing the color of objects in the scene.
    /// </summary>
    public class ColorSetter : MonoBehaviour
    {
        [SerializeField]
        private MeshRenderer _renderer;

        public void SetColor(Color color)
        {
            if (_renderer == null)
            {
                Debug.LogError("MeshRenderer is not assigned.");
                return;
            }

            // Ensure the material is not null before setting the color
            if (_renderer.material != null)
            {
                _renderer.material.color = color;
            }
            else
            {
                Debug.LogError("MeshRenderer's material is not assigned.");
            }
        }
    }
}
