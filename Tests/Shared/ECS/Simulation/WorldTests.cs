using Shared.ECS;
using Shared.ECS.Simulation;
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

    [TickRateMs(100)]
    private class SlowSystem : TestSystem
    {
    }

    [TickRateMs(20)]
    private class TwentyMsSystem : TestSystem
    {
    }

    [Fact]
    public void Systems_Tick_At_Configured_Intervals()
    {
        var clock = new MockClock(new DateTime(2024, 1, 1));
        var slow = new SlowSystem();
        var fast = new TwentyMsSystem();
        var registry = new EntityRegistry();
        var world = new World(new ISystem[] { slow, fast }, clock, registry);

        world.Start();

        // No time advanced: nothing should tick
        Thread.Sleep(5); // let the tick loop run
        Assert.Equal(0, slow.TickCount);
        Assert.Equal(0, fast.TickCount);

        // Advance 20ms: fast system should tick once
        clock.Advance(TimeSpan.FromMilliseconds(20));
        Thread.Sleep(5);
        Assert.Equal(0, slow.TickCount);
        Assert.Equal(1, fast.TickCount);

        // Advance another 20ms: fast ticks again, slow still not
        clock.Advance(TimeSpan.FromMilliseconds(20));
        Thread.Sleep(5);
        Assert.Equal(0, slow.TickCount);
        Assert.Equal(2, fast.TickCount);

        // Advance 60ms (total 100ms): both should tick
        clock.Advance(TimeSpan.FromMilliseconds(60));
        Thread.Sleep(5);
        Assert.Equal(1, slow.TickCount);
        Assert.Equal(3, fast.TickCount);

        // Advance 100ms: both tick again (fast catches up)
        clock.Advance(TimeSpan.FromMilliseconds(100));
        Thread.Sleep(5);
        Assert.Equal(2, slow.TickCount);
        Assert.Equal(8, fast.TickCount);

        world.Stop();
    }

    [Fact]
    public void System_Receives_Correct_DeltaTime()
    {
        var clock = new MockClock(new DateTime(2024, 1, 1));
        var twentyMsSystem = new TwentyMsSystem();
        var registry = new EntityRegistry();
        var world = new World([twentyMsSystem], clock, registry);

        world.Start();

        clock.Advance(TimeSpan.FromMilliseconds(20));
        Thread.Sleep(1000);
        Assert.Equal(0.02f, twentyMsSystem.LastDelta, 2);

        clock.Advance(TimeSpan.FromMilliseconds(40));
        Thread.Sleep(5);
        Assert.Equal(0.04f, twentyMsSystem.LastDelta, 2);

        world.Stop();
    }
}