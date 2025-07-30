using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Scheduling;
using Xunit;

namespace SharedUnitTests.ECS.Simulation;

public class WorldTests
{
    private class TestSystem : ISystem
    {
        public uint TickNumber { get; private set; }
        public float LastDelta { get; private set; }

        public void Update(EntityRegistry registry, uint tickNumber, float deltaTime)
        {
            TickNumber = tickNumber;
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
        var slow = new SlowSystem();
        var fast = new FastSystem();
        var registry = new EntityRegistry();
        var tickRate = TimeSpan.FromMilliseconds(20);
        var scheduler = new MockScheduler();
        var world = new WorldBuilder(registry, scheduler)
            .AddSystem(slow)
            .AddSystem(fast)
            .WithTickRate(tickRate)
            .Build();

        world.Start();

        for (int i = 0; i < 1; i++) scheduler.TickAction!();
        Assert.Equal(0U, slow.TickNumber);
        Assert.Equal(1U, fast.TickNumber);

        for (int i = 0; i < 1; i++) scheduler.TickAction!();
        Assert.Equal(0U, slow.TickNumber);
        Assert.Equal(2U, fast.TickNumber);

        for (int i = 0; i < 3; i++) scheduler.TickAction!();
        Assert.Equal(5U, slow.TickNumber); // Should tick on 5th tick
        Assert.Equal(5U, fast.TickNumber);

        for (int i = 0; i < 5; i++) scheduler.TickAction!();
        Assert.Equal(10U, slow.TickNumber); // Should tick on 10th tick
        Assert.Equal(10U, fast.TickNumber);

        world.Stop();
    }

    [Fact]
    public void System_Receives_Correct_DeltaTime()
    {
        var fast = new FastSystem();
        var registry = new EntityRegistry();
        var tickRate = TimeSpan.FromMilliseconds(20);
        var scheduler = new MockScheduler();
        var world = new WorldBuilder(registry, scheduler)
            .AddSystem(fast)
            .WithTickRate(tickRate)
            .Build();

        world.Start();

        scheduler.TickAction!();
        Assert.Equal(0.02f, fast.LastDelta, 2);

        scheduler.TickAction!();
        Assert.Equal(0.02f, fast.LastDelta, 2);

        world.Stop();
    }
}