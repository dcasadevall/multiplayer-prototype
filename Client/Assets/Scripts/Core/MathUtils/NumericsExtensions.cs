using System.Numerics;

namespace Core.MathUtils
{
    public static class NumericsExtensions
    {
        public static UnityEngine.Vector3 ToUnityVector3(this System.Numerics.Vector3 vector3)
        {
            return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
        }
        
        public static UnityEngine.Vector2 ToUnityVector2(this System.Numerics.Vector2 vector3)
        {
            return new UnityEngine.Vector2(vector3.X, vector3.Y);
        }
        
        public static System.Numerics.Vector2 ToNumericsVector2(this UnityEngine.Vector2 vector2)
        {
            return new System.Numerics.Vector2(vector2.x, vector2.y);
        }

        public static System.Numerics.Vector3 ToNumericsVector3(this UnityEngine.Vector3 vector3)
        {
            return new System.Numerics.Vector3(vector3.x, vector3.y, vector3.z);
        }
        
        public static System.Numerics.Vector2 Normalized(this System.Numerics.Vector2 vector)
        {
            return vector.ToUnityVector2().normalized.ToNumericsVector2();
        }
        
        public static UnityEngine.Quaternion ToUnityQuaternion(this System.Numerics.Quaternion quaternion)
        {
            return new UnityEngine.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }
    }
}