using System;

namespace Shared.ECS.Simulation
{
    public static class TickExtensions
    {
        /// <summary>
        /// Returns the number of ticks represented by the given TimeSpan.
        /// This assumes a fixed tick rate defined by SharedConstants.WorldTicksPerSecond.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static uint ToNumTicks(this TimeSpan timeSpan)
        {
            // Calculate the number of ticks by dividing the total seconds by the tick rate in seconds.
            // The tick rate is expected to be in seconds, so we convert it to seconds by
            // multiplying by 1000 to convert milliseconds to seconds.
            return (uint)(timeSpan.TotalSeconds * SharedConstants.WorldTicksPerSecond);
        }
    }
}