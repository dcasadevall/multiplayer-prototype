# Client-Side ECS Replication System

This directory contains the Unity client-side implementation of the ECS replication system that works with the server's `IWorldSnapshotConsumer` interface.

## Overview

The client-side replication system consists of several key components:

### Core Components

1. **`ClientWorldManager`** - Main coordinator that manages the ECS world lifecycle using DI
2. **`UnityServiceProvider`** - Unity-compatible service provider for dependency injection
3. **`ClientReplicationSystem`** - ECS system that processes incoming server snapshots
4. **`EntityViewSystem`** - ECS system that renders entities as Unity GameObjects
5. **`UnityMessageReceiver`** - Unity-specific implementation of `IMessageReceiver`

### Architecture

```
Unity GameManager
    ↓
ClientWorldManager (MonoBehaviour)
    ↓
UnityServiceProvider (DI Container)
    ↓
ECS World (built with WorldBuilder)
    ├── ClientReplicationSystem (uses IWorldSnapshotConsumer)
    └── EntityViewSystem (renders entities as GameObjects)
    ↓
UnityMessageReceiver (handles network messages)
```

## Setup Instructions

### 1. Basic Integration

The system is automatically integrated into your existing `GameManager`. Simply add the `ClientWorldManager` component:

```csharp
// In your GameManager
private void Awake()
{
    // Initialize the ECS world manager
    _clientWorldManager = gameObject.AddComponent<ClientWorldManager>();
}
```

### 2. Configuration

Configure the server connection in the Unity Inspector:

- **Server Address**: The server's IP address (default: "localhost")
- **Server Port**: The server's port number (default: 9050)
- **Ticks Per Second**: ECS world update rate (default: 30)

### 3. Network Integration

The `UnityMessageReceiver` is a placeholder that needs to be integrated with your networking library (e.g., LiteNetLib). Key methods to implement:

```csharp
public void ConnectToServer(string serverAddress, int port)
{
    // TODO: Implement connection logic
}

public void HandleMessageReceived(MessageType messageType, byte[] data)
{
    // This method is called when network messages are received
    OnMessageReceived?.Invoke(messageType, data);
}
```

## How It Works

### 1. World Initialization

When `ClientWorldManager` starts:

1. Creates a `UnityServiceProvider` for dependency injection
2. Registers all services (EntityRegistry, IScheduler, ISystem implementations, etc.)
3. Uses `WorldBuilder` to create the world with the specified frequency
4. Adds all registered systems to the world using DI
5. Builds and starts the world with fixed timestep simulation

### 2. Snapshot Processing

When a snapshot is received from the server:

1. `UnityMessageReceiver` receives the network message
2. `ClientReplicationSystem` processes the message
3. `JsonWorldSnapshotConsumer` deserializes the snapshot
4. Entities and components are updated in the `EntityRegistry`
5. `EntityViewSystem` creates/updates Unity GameObjects

### 3. Entity Rendering

The `EntityViewSystem` automatically:

- Creates Unity GameObjects for entities with `PositionComponent`
- Updates GameObject positions based on ECS data
- Removes GameObjects for entities that no longer exist
- Handles cleanup when the world is destroyed

## Usage Examples

### Accessing the ECS World

```csharp
// Get the entity registry
var registry = gameManager.EntityRegistry;

// Get the world
var world = gameManager.World;

// Query for entities
foreach (var entity in registry.GetAll())
{
    if (entity.Has<PositionComponent>())
    {
        var position = entity.Get<PositionComponent>().Value;
        Debug.Log($"Entity at position: {position}");
    }
}
```

### Adding Custom Systems

```csharp
// In ClientWorldManager.InitializeWorld()
var customSystem = new MyCustomSystem();
_world.AddSystem(customSystem);
```

### Custom Entity Views

Extend `EntityViewSystem` to support custom rendering:

```csharp
private void CreateEntityView(Entity entity, EntityRegistry registry)
{
    // Load different prefabs based on entity tags or components
    if (entity.HasTag("Player"))
    {
        var prefab = Resources.Load<GameObject>("PlayerPrefab");
        var view = Instantiate(prefab);
        // Setup view...
    }
    else if (entity.HasTag("Projectile"))
    {
        var prefab = Resources.Load<GameObject>("ProjectilePrefab");
        var view = Instantiate(prefab);
        // Setup view...
    }
}
```

## Next Steps

1. **Implement Network Integration**: Connect `UnityMessageReceiver` to your networking library
2. **Add Input Systems**: Create systems to send player input to the server
3. **Enhance Entity Views**: Add support for different entity types and visual effects
4. **Add Interpolation**: Implement client-side prediction and interpolation for smooth movement
5. **Add Debug Tools**: Create visual debugging tools for the ECS world

## Troubleshooting

### Common Issues

1. **Entities not appearing**: Check that entities have `PositionComponent` and are being replicated
2. **Network not working**: Ensure `UnityMessageReceiver` is properly integrated with your networking library
3. **Performance issues**: Adjust the tick rate or optimize the `EntityViewSystem`

### Debug Logging

The system includes comprehensive debug logging. Check the Unity Console for messages starting with:
- `ClientWorldManager:`
- `ClientReplicationSystem:`
- `EntityViewSystem:`
- `UnityMessageReceiver:` 