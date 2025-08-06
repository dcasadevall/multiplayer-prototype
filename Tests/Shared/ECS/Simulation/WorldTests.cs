using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.ECS.TickSync;
using Shared.Scheduling;
using Xunit;

namespace SharedUnitTests.ECS.Simulation
{
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
        private class SlowSystem : TestSystem
        {
        }

        [TickInterval(1)]
        private class FastSystem : TestSystem
        {
        }

        /// <summary>
        /// A mock scheduler that allows manual ticking for deterministic tests.
        /// </summary>
        private class MockScheduler : IScheduler
        {
            public Action? TickAction { get; private set; }

            public IDisposable ScheduleAtFixedRate(Action task, TimeSpan initialDelay, TimeSpan period,
                CancellationToken cancellationToken = default)
            {
                TickAction = task;
                return new DummyDisposable();
            }

            private class DummyDisposable : IDisposable
            {
                public void Dispose()
                {
                }
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
            var tickSync = new TickSync();
            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(slow)
                .AddSystem(fast)
                .WithTickRate(tickRate)
                .Build();

            world.Start();

            // Tick 1
            tickSync.ClientTick = 1;
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(1U, fast.TickNumber);

            // Tick 2
            tickSync.ClientTick = 2;
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(2U, fast.TickNumber);

            // Tick 3
            tickSync.ClientTick = 3;
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(3U, fast.TickNumber);

            // Tick 4
            tickSync.ClientTick = 4;
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(4U, fast.TickNumber);

            // Tick 5 (slow system should run now)
            tickSync.ClientTick = 5;
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber); // Should tick on 5th tick
            Assert.Equal(5U, fast.TickNumber);

            // Tick 6
            tickSync.ClientTick = 6;
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(6U, fast.TickNumber);

            // Tick 7
            tickSync.ClientTick = 7;
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(7U, fast.TickNumber);

            // Tick 8
            tickSync.ClientTick = 8;
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(8U, fast.TickNumber);

            // Tick 9
            tickSync.ClientTick = 9;
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(9U, fast.TickNumber);

            // Tick 10 (slow system should run again)
            tickSync.ClientTick = 10;
            scheduler.TickAction!();
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
            var tickSync = new TickSync();
            var world = new WorldBuilder(registry, tickSync, scheduler)
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

        [Fact]
        public void World_Increments_TickNumber_Only_In_Server_Mode()
        {
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();
            var tickSync = new TickSync();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(fast)
                .WithTickRate(tickRate)
                .WithWorldMode(WorldMode.Server)
                .Build();

            world.Start();

            Assert.Equal(0U, world.CurrentTickIndex);
            scheduler.TickAction!();
            Assert.Equal(1U, world.CurrentTickIndex);
            scheduler.TickAction!();
            Assert.Equal(2U, world.CurrentTickIndex);
            scheduler.TickAction!();
            Assert.Equal(3U, world.CurrentTickIndex);

            world.Stop();
        }

        [Fact]
        public void World_Sets_TickNumber_To_ClientTick_In_Client_Mode()
        {
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();
            var tickSync = new TickSync();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(fast)
                .WithTickRate(tickRate)
                .WithWorldMode(WorldMode.Client)
                .Build();

            world.Start();

            tickSync.ClientTick = 10;
            scheduler.TickAction!();
            Assert.Equal(10U, world.CurrentTickIndex);

            tickSync.ClientTick = 42;
            scheduler.TickAction!();
            Assert.Equal(42U, world.CurrentTickIndex);

            tickSync.ClientTick = 99;
            scheduler.TickAction!();
            Assert.Equal(99U, world.CurrentTickIndex);

            world.Stop();
        }
    }
}