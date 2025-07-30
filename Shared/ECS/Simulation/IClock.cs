namespace Shared.ECS.Simulation;

/// <summary>
/// Abstraction for providing the current time, to allow for testable ticking.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}