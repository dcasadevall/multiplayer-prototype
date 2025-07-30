namespace Shared.Clock;

/// <summary>
/// Default implementation of <see cref="IClock"/> that returns the real system UTC time.
/// </summary>
public class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
