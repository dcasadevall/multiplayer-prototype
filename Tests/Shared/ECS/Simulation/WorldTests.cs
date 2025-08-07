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
                .WithWorldMode(WorldMode.Server) // Default to server mode for predictable behavior
                .Build();

            world.Start();

            // Tick 0
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber); // Runs on tick 0
            Assert.Equal(0U, fast.TickNumber); // Runs on tick 0
            Assert.Equal(1U, world.CurrentTickIndex);

            // Tick 1
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber); // Doesn't run on tick 1
            Assert.Equal(1U, fast.TickNumber); // Runs on tick 1
            Assert.Equal(2U, world.CurrentTickIndex);

            // Tick 2
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(2U, fast.TickNumber);
            Assert.Equal(3U, world.CurrentTickIndex);

            // Tick 3
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(3U, fast.TickNumber);
            Assert.Equal(4U, world.CurrentTickIndex);

            // Tick 4
            scheduler.TickAction!();
            Assert.Equal(0U, slow.TickNumber);
            Assert.Equal(4U, fast.TickNumber);
            Assert.Equal(5U, world.CurrentTickIndex);

            // Tick 5
            scheduler.TickAction!();
            Assert.Equal(5U, slow.TickNumber); // Runs on tick 5
            Assert.Equal(5U, fast.TickNumber);
            Assert.Equal(6U, world.CurrentTickIndex);

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
        public void World_Increments_TickNumber_Each_Update_In_Server_Mode()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();

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
        public void World_Uses_TickSync_ClientTick_In_Client_Mode()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var fast = new FastSystem();
            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(fast)
                .WithTickRate(tickRate)
                .WithWorldMode(WorldMode.Client)
                .Build();

            world.Start();

            // Manually set ClientTick values to simulate TickSync behavior
            tickSync.ClientTick = 10;
            scheduler.TickAction!();
            Assert.Equal(10U, world.CurrentTickIndex);

            tickSync.ClientTick = 15;
            scheduler.TickAction!();
            Assert.Equal(15U, world.CurrentTickIndex);

            tickSync.ClientTick = 20;
            scheduler.TickAction!();
            Assert.Equal(20U, world.CurrentTickIndex);

            world.Stop();
        }

        [Fact]
        public void ClientTickSystem_Sets_ClientTick_To_Current_World_Tick()
        {
            var tickSync = new Shared.ECS.TickSync.TickSync();
            var connection = Substitute.For<IClientConnection>();
            connection.PingMs.Returns(0); // No ping for simplicity
            var clientTickSystem = new Shared.ECS.TickSync.ClientTickSystem(tickSync, connection);

            var registry = new EntityRegistry();
            var tickRate = TimeSpan.FromMilliseconds(20);
            var scheduler = new MockScheduler();

            var world = new WorldBuilder(registry, tickSync, scheduler)
                .AddSystem(clientTickSystem)
                .WithTickRate(tickRate)
                .WithWorldMode(WorldMode.Client)
                .Build();

            world.Start();

            // The world tick number in client mode is driven by tickSync.ClientTick
            // ClientTickSystem sets tickSync.ClientTick = tickNumber at the start of Update()
            
            // Tick 1: World calls system with tickNumber=0, system sets ClientTick=0, then CorrectForDrift may modify it
            scheduler.TickAction!();
            // After first tick, ClientTick should be set to 0 initially, then potentially modified by drift correction
            // Since there's no ServerTickComponent, the system returns early, so ClientTick stays 0
            Assert.Equal(0U, tickSync.ClientTick);
            Assert.Equal(0U, world.CurrentTickIndex); // World uses ClientTick value

            world.Stop();
        }
    }
}