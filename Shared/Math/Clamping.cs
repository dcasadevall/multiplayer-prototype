namespace Shared.Math
{
    public static class Clamping
    {
        /// <summary>
        /// Returns the value clamped between min and max.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Returns the maximum of two integers.
        /// </summary>
        public static int Max(int a, int b)
        {
            if (a > b) return a;
            return b;
        }

        /// <summary>
        /// Returns the minimum of two integers.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int Min(int a, int b)
        {
            if (a < b) return a;
            return b;
        }
    }
}