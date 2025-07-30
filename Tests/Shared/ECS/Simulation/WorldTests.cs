using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Scheduling;
using Xunit;

namespace SharedUnitTests.ECS.Simulation;

public class WorldTests
{
    private class TestSystem : ISystem
    {
        public int TickCount { get; private set; }
        public float LastDelta { get; private set; }

        public void Update(EntityRegistry registry, float deltaTime)
        {
            TickCount++;
            LastDelta = deltaTime;
        }
    }

    [TickInterval(5)]
    private class SlowSystem : TestSystem { }

    [TickInterval(1)]
    private class FastSystem : TestSystem { }

    /// <summary>
    /// A mock scheduler that allows manual ticking for deterministic tests.
    /// </summary>
    private class MockScheduler : IScheduler
    {
        public Action? TickAction { get; private set; }
        public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            TickAction = task;
            return new DummyDisposable();
        }

        private class DummyDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    [Fact]
    public void Systems_Tick_At_Configured_Intervals()
    {
        var clock = new MockClock(new DateTime(2024, 1, 1));
        var slow = new SlowSystem();
        var fast = new FastSystem();
        var registry = new EntityRegistry();
        var tickRate = TimeSpan.FromMilliseconds(20);
        var scheduler = new MockScheduler();
        var world = new World(new ISystem[] { slow, fast }, clock, registry, tickRate, scheduler);

        world.Start();

        for (int i = 0; i < 1; i++) scheduler.TickAction!();
        Assert.Equal(0, slow.TickCount);
        Assert.Equal(1, fast.TickCount);

        for (int i = 0; i < 1; i++) scheduler.TickAction!();
        Assert.Equal(0, slow.TickCount);
        Assert.Equal(2, fast.TickCount);

        for (int i = 0; i < 3; i++) scheduler.TickAction!();
        Assert.Equal(1, slow.TickCount); // Should tick on 5th tick
        Assert.Equal(5, fast.TickCount);

        for (int i = 0; i < 5; i++) scheduler.TickAction!();
        Assert.Equal(2, slow.TickCount); // Should tick on 10th tick
        Assert.Equal(10, fast.TickCount);

        world.Stop();
    }

    [Fact]
    public void System_Receives_Correct_DeltaTime()
    {
        var clock = new MockClock(new DateTime(2024, 1, 1));
        var fast = new FastSystem();
        var registry = new EntityRegistry();
        var tickRate = TimeSpan.FromMilliseconds(20);
        var scheduler = new MockScheduler();
        var world = new World([fast], clock, registry, tickRate, scheduler);

        world.Start();

        scheduler.TickAction!();
        Assert.Equal(0.02f, fast.LastDelta, 2);

        scheduler.TickAction!();
        Assert.Equal(0.02f, fast.LastDelta, 2);

        world.Stop();
    }
}