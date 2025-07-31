namespace Core.MathUtils
{
    public static class NumericsExtensions
    {
        public static UnityEngine.Vector3 ToUnityVector3(this System.Numerics.Vector3 vector3)
        {
            return new UnityEngine.Vector3(vector3.X, vector3.Y, vector3.Z);
        }
    }
}