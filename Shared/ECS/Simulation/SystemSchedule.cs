namespace Shared.ECS.Simulation;

/// <summary>
/// Represents the scheduling metadata for a single system within a world,
/// enabling variable tick rates per system. Each <see cref="SystemSchedule"/>
/// tracks the system instance, its desired tick interval (in milliseconds),
/// and the last time it was ticked. The world's ticker uses this information
/// to determine when each system should be updated, allowing systems to run
/// at different rates (e.g., via a <c>[TickRateMs]</c> attribute).
/// 
/// Usage:
/// - Each system is wrapped in a <see cref="SystemSchedule"/> with its tick interval.
/// - On each world tick, the ticker checks if enough time has elapsed since <see cref="LastTick"/>
///   to run the system again (i.e., if <c>now - LastTick &gt;= IntervalMs</c>).
/// - If so, the system's <c>Update</c> method is called, and <see cref="LastTick"/> is updated.
/// This enables precise, per-system ticking and clean separation of each world's simulation step.
/// </summary>
internal class SystemSchedule(ISystem system, int intervalMs)
{
    /// <summary>
    /// The system instance to be scheduled.
    /// </summary>
    public ISystem System { get; } = system;

    /// <summary>
    /// The tick interval for this system, in milliseconds.
    /// The system will only be updated if at least this much time has elapsed since the last tick.
    /// </summary>
    public int IntervalMs { get; } = intervalMs;

    /// <summary>
    /// The last time this system was ticked (in UTC).
    /// Used to determine if the system is due for another update.
    /// </summary>
    public DateTime LastTick { get; set; } = DateTime.UtcNow;
}