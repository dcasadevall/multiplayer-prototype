# Networking Replication System

This folder contains the server-side networking and replication system for the multiplayer prototype. The replication system is responsible for synchronizing the game state between the server and connected clients.

## Overview

The replication system uses a **snapshot-based approach** to ensure all clients receive consistent and up-to-date game state. It works by:

1. **Identifying replicated entities** - Entities marked with `ReplicatedEntityComponent`
2. **Serializing component state** - Components implementing `ISerializableComponent`
3. **Transmitting snapshots** - Sending binary data to all connected clients
4. **Maintaining consistency** - Using reliable, ordered delivery for critical state

## Core Components

### EntityReplicator.cs
The main replication engine that handles:
- **Snapshot creation** - Serializes all replicated entities and their components
- **Network transmission** - Sends snapshots to all connected peers
- **Binary serialization** - Uses LiteNetLib's `NetDataWriter` for efficient data transmission

**Key Methods:**
- `SendSnapshotToAll()` - Broadcasts current game state to all clients
- `CreateSnapshot()` - Serializes entity and component data
- `SendSnapshotTo()` - Sends snapshot to a specific peer

### ISerializableComponent.cs
Interface that components must implement to be included in replication:

```csharp
public interface ISerializableComponent : IComponent
{
    void Serialize(NetDataWriter writer);
    void Deserialize(NetDataReader reader);
}
```

**Requirements:**
- Components must implement both `Serialize()` and `Deserialize()` methods
- `Serialize()` converts component state to binary format
- `Deserialize()` reconstructs component state from binary data

### ReplicatedEntityComponent.cs
Marker component that identifies entities for replication:

```csharp
public class ReplicatedEntityComponent : IComponent
{
    // Empty marker component
}
```

**Usage:**
- Add this component to any entity that should be synchronized with clients
- Only entities with this component will be included in snapshots

## How Replication Works

### 1. Entity Selection
```csharp
var entities = _entityManager
    .GetAll()
    .Where(e => e.Has<ReplicatedEntityComponent>())
    .ToList();
```

### 2. Component Filtering
```csharp
var serializableComponents = entity
    .GetAllComponents()
    .Where(x => x is ISerializableComponent)
    .Cast<ISerializableComponent>()
    .ToList();
```

### 3. Snapshot Serialization
The system creates a binary snapshot containing:
- **Entity count** - Number of replicated entities
- **Entity IDs** - Unique identifiers for each entity
- **Component data** - Serialized state of all `ISerializableComponent` instances

### 4. Network Transmission
```csharp
peer.Send(snapshotData, DeliveryMethod.ReliableOrdered);
```

Uses reliable, ordered delivery to ensure:
- **No packet loss** - Critical for game state consistency
- **Correct ordering** - Prevents state corruption from out-of-order packets

## Implementation Example

### Making a Component Replicatable

```csharp
public class PositionComponent : ISerializableComponent
{
    public Vector3 Value;

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Value.X);
        writer.Put(Value.Y);
        writer.Put(Value.Z);
    }

    public void Deserialize(NetDataReader reader)
    {
        Value = new Vector3(
            reader.GetFloat(),
            reader.GetFloat(),
            reader.GetFloat()
        );
    }
}
```

### Replicating an Entity

```csharp
// Create entity
var entity = registry.CreateEntity();

// Add replicated components
entity.AddComponent(new ReplicatedEntityComponent());
entity.AddComponent(new PositionComponent(new Vector3(10, 20, 30)));

// Entity will now be included in replication snapshots
```

## Integration with ECS

The replication system integrates seamlessly with the ECS architecture:

- **Systems** can modify component state normally
- **Replication** automatically picks up changes through snapshots
- **No coupling** between game logic and networking code
- **Declarative** - Components opt-in to replication via interfaces

## Performance Considerations

- **Snapshot frequency** - Balance between responsiveness and bandwidth
- **Component filtering** - Only replicate necessary data
- **Binary efficiency** - Use appropriate data types for serialization
- **Network optimization** - Consider compression for large snapshots

## Future Enhancements

Potential improvements to the replication system:

- **Delta compression** - Send only changed data instead of full snapshots
- **Interest management** - Replicate only entities relevant to each client
- **Prediction and reconciliation** - Client-side prediction with server correction
- **Bandwidth optimization** - Adaptive quality based on connection speed
