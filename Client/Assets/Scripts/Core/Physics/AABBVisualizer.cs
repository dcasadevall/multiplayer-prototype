using Core.MathUtils;
using Shared.Physics;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace Core.Physics
{
    /// <summary>
    /// A MonoBehaviour that draws a wireframe box to visualize a <see cref="WorldAABBComponent"/>.
    /// This is intended to be used for debugging purposes in the Unity Editor.
    /// </summary>
    public class AABBVisualizer : MonoBehaviour
    {
        /// <summary>
        /// The center of the box, in world space.
        /// </summary>
        public Vector3 Center;
        /// <summary>
        /// The size of the box, in world space.
        /// </summary>
        public Vector3 Size;
        /// <summary>
        /// The color of the wireframe box.
        /// </summary>
        public Color Color = Color.green;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color;
            Gizmos.DrawWireCube(Center.ToUnityVector3(), Size.ToUnityVector3());
        }
    }
}

