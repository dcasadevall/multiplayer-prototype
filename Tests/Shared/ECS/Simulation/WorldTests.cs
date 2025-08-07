using NSubstitute;
using Shared.ECS;
using Shared.ECS.Simulation;
using Shared.Networking;
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
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var slow = new SlowSystem();
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();
            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(slow)
                .AddSystem(fast)
                .WithTickRate(tickRate)
                .Build();

            world.Start();

            // Tick 1
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(1U, fast.TickNumber);

            // Tick 2
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(2U, fast.TickNumber);

            // Tick 3
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(3U, fast.TickNumber);

            // Tick 4
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(4U, fast.TickNumber);

            // Tick 5 (slow system should run now)
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber); // Should tick on 5th tick
            Assert.Equal(5U, fast.TickNumber);

            // Tick 6
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(6U, fast.TickNumber);

            // Tick 7
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(7U, fast.TickNumber);

            // Tick 8
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(8U, fast.TickNumber);

            // Tick 9
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber);
            Assert.Equal(9U, fast.TickNumber);

            // Tick 10 (slow system should run again)
            scheduler.TickAction!();
            Assert.Equal(10U, slow.TickNumber); // Should tick on 10th tick
            Assert.Equal(10U, fast.TickNumber);

            world.Stop();
        }

        [Fact]
        public void System_Receives_Correct_DeltaTime()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();
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
        public void World_Increments_TickNumber_Each_Update()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(fast)
                .WithTickRate(tickRate)
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
        public void ClientTickSystem_Sets_ClientTick_To_Current_World_Tick()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var connection = Substitute.For<IClientConnection>();
            var clientTickSystem = new Shared.ECS.TickSync.ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(clientTickSystem)
                .WithTickRate(tickRate)
                .Build();

            world.Start();

            // Tick 1
            scheduler.TickAction!();
            Assert.Equal(1U, tickSync.ClientTick);

            // Tick 2
            scheduler.TickAction!();
            Assert.Equal(2U, tickSync.ClientTick);

            // Tick 3
            scheduler.TickAction!();
            Assert.Equal(3U, tickSync.ClientTick);

            world.Stop();
        }
    }
}