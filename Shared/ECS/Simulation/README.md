# Fixed Timestep ECS Simulation

This directory contains the simulation framework for the ECS world, implementing deterministic fixed timestep simulation.

## 🎯 Fixed Timestep Benefits

- **Deterministic**: The same inputs always produce the same results
- **Stable Physics**: No tunneling or erratic behavior due to variable timesteps
- **Clear Event Scheduling**: Discrete tick indexes for unambiguous timing
- **Perfect for Multiplayer**: Consistent simulation across all clients
- **Network Synchronization**: Easy to tag messages with tick indexes
- **Replay System**: Record game state at specific ticks for debugging

## 🏗️ Architecture

### Core Classes

#### `World`
The main simulation class that runs at a constant rate:
```csharp
var world = new World(systems, clock, entityRegistry, TimeSpan.FromMilliseconds(33.33));
```

**Key Properties:**
- `CurrentTickIndex`: The current simulation step (starts at 1)
- `FixedDeltaTime`: The constant time step (e.g., 0.0333s for 30Hz)
- `TickRate`: The time between ticks

**Events:**
- `OnFirstTick`: Raised when the world starts (tick index = 1)
- `OnTick`: Raised on each tick before systems are updated

#### `TickIntervalAttribute`
Controls how often a system runs:
```csharp
[TickInterval(1)]   // Run every tick
[TickInterval(10)]  // Run every 10th tick
[TickInterval(60)]  // Run every 60th tick (once per second at 60Hz)
```

#### `WorldBuilder`
Fluent builder for creating worlds:
```csharp
var world = new WorldBuilder(clock, entityRegistry)
    .WithFrequency(30)  // 30Hz
    .AddSystem(new MovementSystem())
    .AddSystem(new HealthSystem())
    .Build();
```

## 📝 Usage Examples

### Basic Setup
```csharp
var entityRegistry = new EntityRegistry();
var clock = new SystemClock();

var world = new WorldBuilder(clock, entityRegistry)
    .WithFrequency(30)  // 30Hz
    .AddSystem(new MovementSystem())
    .AddSystem(new HealthSystem())
    .Build();

// Set up first tick event
world.OnFirstTick += () =>
{
    SceneLoader.Load("scene.json", entityRegistry);
};

world.Start();
```

### System Implementation
```csharp
[TickInterval(1)]  // Run every tick for smooth movement
public class MovementSystem : ISystem
{
    public void Update(EntityRegistry entityRegistry, float deltaTime)
    {
        // deltaTime is always the same (e.g., 0.0333s at 30Hz)
        var entities = entityRegistry.GetAll()
            .Where(e => e.Has<PositionComponent>() && e.Has<VelocityComponent>());

        foreach (var entity in entities)
        {
            if (entity.TryGet<PositionComponent>(out var position) &&
                entity.TryGet<VelocityComponent>(out var velocity))
            {
                position.Value += velocity.Value * deltaTime;
            }
        }
    }
}

[TickInterval(10)]  // Run every 10th tick for health updates
public class HealthSystem : ISystem
{
    public void Update(EntityRegistry entityRegistry, float deltaTime)
    {
        // Health regeneration logic
    }
}
```

### Event Scheduling
```csharp
world.OnTick += (tickIndex) =>
{
    // Schedule events based on tick index
    if (tickIndex % 300 == 0)  // Every 10 seconds at 30Hz
    {
        Console.WriteLine($"Tick {tickIndex} - 10 seconds elapsed");
    }
    
    if (tickIndex == 1000)  // Specific tick
    {
        Console.WriteLine("1000th tick reached!");
    }
};
```

## 🎮 Tick Index Benefits

### Deterministic Events
```csharp
// A 5-second cooldown at 30Hz is exactly 150 ticks
const uint COOLDOWN_TICKS = 150;

if (tickIndex - lastAbilityUse >= COOLDOWN_TICKS)
{
    // Ability is ready
}
```

### Network Synchronization
```csharp
// Tag network messages with tick index
public class NetworkMessage
{
    public uint TickIndex { get; set; }
    public byte[] Data { get; set; }
}

// Process messages in correct order
if (message.TickIndex <= world.CurrentTickIndex)
{
    ProcessMessage(message);
}
```

### Replay System
```csharp
// Record game state at specific ticks
public class GameState
{
    public uint TickIndex { get; set; }
    public Dictionary<EntityId, Entity> Entities { get; set; }
}

// Replay from any point
public void ReplayFromTick(uint startTick)
{
    var state = GetStateAtTick(startTick);
    // Restore world to that state
}
```

## 🧪 Testing

The `WorldTests` demonstrates:
- Tick index progression
- System scheduling accuracy
- Event handling
- Deterministic behavior

## 🎯 Best Practices

1. **Choose appropriate tick intervals**: Use `[TickInterval(1)]` for movement/physics, higher values for infrequent updates
2. **Leverage tick indexes**: Use them for event scheduling, network synchronization, and debugging
3. **Keep systems focused**: Each system should handle one aspect of gameplay
4. **Test determinism**: Ensure the same inputs produce the same results
5. **Monitor performance**: Fixed timestep can be CPU-intensive, profile your systems
6. **Use the first tick**: Perfect place for initialization like loading scenes or spawning entities

## 🔧 Performance Considerations

- **CPU Usage**: Fixed timestep runs at a constant rate regardless of load
- **System Intervals**: Use higher intervals for systems that don't need frequent updates
- **Profiling**: Monitor system execution times to ensure they complete within the tick budget
- **Async Operations**: Avoid blocking operations in system updates 