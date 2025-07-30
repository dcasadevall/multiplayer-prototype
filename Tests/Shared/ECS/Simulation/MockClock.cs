using Shared.Clock;
using Shared.ECS.Simulation;

namespace SharedUnitTests.ECS.Simulation;

/// <summary>
/// A mock clock for tests, allowing manual advancement of time.
/// </summary>
public class MockClock(DateTime? start = null) : IClock
{
    private DateTime _now = start ?? DateTime.UtcNow;
    public DateTime UtcNow => _now;
    public void Advance(TimeSpan span) => _now = _now.Add(span);
}