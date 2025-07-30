namespace Shared.ECS.Simulation;

/// <summary>
/// Attribute to specify the desired tick interval (in milliseconds) for a system.
/// When applied to a system class, this attribute informs the world's ticker how often
/// the system should be updated. If not present, a default tick rate is used.
/// 
/// Usage:
/// <code>
/// [TickRateMs(100)] // System will be updated every 100ms
/// public class MySystem : ISystem { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TickRateMsAttribute(int intervalMs) : Attribute
{
    /// <summary>
    /// The tick interval in milliseconds for the decorated system.
    /// </summary>
    public int IntervalMs { get; } = intervalMs;
}