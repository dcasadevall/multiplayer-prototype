using Xunit;
using Shared.ECS;
using Shared.ECS.Components;
using Shared.ECS.Simulation;
using Shared.ECS.Systems;
using System.Numerics;
using Shared.Clock;
using Shared.Scheduling;

namespace SharedUnitTests;

public class WorldTests
{
    private class MockScheduler : IScheduler
    {
        public Action? TickAction { get; private set; }
        public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period, CancellationToken cancellationToken = default)
        {
            TickAction = task;
            return new DummyDisposable();
        }
        
        private class DummyDisposable : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public void Constructor_ShouldSetCorrectTickRate()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var tickRate = TimeSpan.FromMilliseconds(50); // 20Hz
        var scheduler = new MockScheduler();

        // Act
        var world = new World([], clock, registry, tickRate, scheduler);

        // Assert
        Assert.Equal(tickRate, world.TickRate);
        Assert.Equal(0.05f, world.FixedDeltaTime, 0.001f);
        Assert.Equal(0u, world.CurrentTickIndex);
    }

    [Fact]
    public void Start_ShouldBeginSimulation()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var scheduler = new MockScheduler();
        var world = new World([], clock, registry, TimeSpan.FromMilliseconds(100), scheduler);

        // Act
        world.Start();
        scheduler.TickAction!();

        // Assert
        Assert.True(world.CurrentTickIndex > 0);

        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void OnFirstTick_ShouldBeRaisedOnFirstTick()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var scheduler = new MockScheduler();
        var world = new World([], clock, registry, TimeSpan.FromMilliseconds(50), scheduler);
        var firstTickRaised = false;

        world.OnFirstTick += () => firstTickRaised = true;

        // Act
        world.Start();
        scheduler.TickAction!();

        // Assert
        Assert.True(firstTickRaised);

        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void OnTick_ShouldBeRaisedOnEachTick()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var scheduler = new MockScheduler();
        var world = new World([], clock, registry, TimeSpan.FromMilliseconds(50), scheduler);
        var tickCount = 0;
        var lastTickIndex = 0u;

        world.OnTick += (tickIndex) =>
        {
            tickCount++;
            lastTickIndex = tickIndex;
        };

        // Act
        world.Start();
        scheduler.TickAction!();
        scheduler.TickAction!();

        // Assert
        Assert.True(tickCount == 2);
        Assert.True(lastTickIndex == 2);

        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void Systems_ShouldRunAtCorrectIntervals()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var movementSystem = new TestMovementSystem();
        var healthSystem = new TestHealthSystem();
        var scheduler = new MockScheduler();

        var world = new World(
            [movementSystem, healthSystem], 
            clock, 
            registry, 
            TimeSpan.FromMilliseconds(50), // 20Hz
            scheduler
        );

        // Act
        world.Start();
        for (int i = 0; i < 6; i++) scheduler.TickAction!();

        // Assert
        // Movement system should run every tick (interval = 1)
        Assert.True(movementSystem.UpdateCount == 6);
        
        // Health system should run every 10 ticks (interval = 10)
        // At 20Hz, 300ms = 6 ticks, so health system should run 0-1 times
        Assert.True(healthSystem.UpdateCount <= 1);

        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void FixedDeltaTime_ShouldBeConsistent()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var scheduler = new MockScheduler();
        var world = new World([], clock, registry, TimeSpan.FromMilliseconds(33.33f), scheduler); // 30Hz

        // Act & Assert
        Assert.Equal(0.03333f, world.FixedDeltaTime, 0.001f);

        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void Stop_ShouldStopSimulation()
    {
        // Arrange
        var registry = new EntityRegistry();
        var clock = new SystemClock();
        var scheduler = new MockScheduler();
        var world = new World([], clock, registry, TimeSpan.FromMilliseconds(50), scheduler);

        world.Start();
        scheduler.TickAction!();
        var tickIndexBeforeStop = world.CurrentTickIndex;

        // Act
        world.Stop();
        scheduler.TickAction!();

        // Assert
        Assert.Equal(tickIndexBeforeStop, world.CurrentTickIndex);

        // Cleanup
        world.Dispose();
    }

    // Test systems for verifying tick intervals
    private class TestMovementSystem : ISystem
    {
        public int UpdateCount { get; private set; }

        public void Update(EntityRegistry entityRegistry, float deltaTime)
        {
            UpdateCount++;
        }
    }

    [TickInterval(10)]
    private class TestHealthSystem : ISystem
    {
        public int UpdateCount { get; private set; }

        public void Update(EntityRegistry entityRegistry, float deltaTime)
        {
            UpdateCount++;
        }
    }
}
