namespace Shared.Math
{
    public static class Lerping
    {
        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="a">The start value.</param>
        /// <param name="b">The end value.</param>
        /// <param name="t">The interpolation weight (usually between 0.0 and 1.0).</param>
        /// <returns>The interpolated value.</returns>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="a">The start value.</param>
        /// <param name="b">The end value.</param>
        /// <param name="t">The interpolation weight (usually between 0.0 and 1.0).</param>
        /// <returns>The interpolated value.</returns>
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Linearly interpolates between two unsigned integers.
        /// </summary>
        /// <param name="a">The start value.</param>
        /// <param name="b">The end value.</param>
        /// <param name="t">The interpolation weight (usually between 0.0 and 1.0).</param>
        /// <returns>The interpolated value.</returns>
        public static uint Lerp(uint a, uint b, float t)
        {
            return (uint)(a + (b - a) * t);
        }
    }
}