# Multiplayer Prototype: ECS Architecture Overview

## 🏗️ Project Structure

```
/Shared/           # Shared ECS logic (used by both server and client)
  /ECS/            # Core ECS interfaces and base types
    IComponent.cs  # Marker interface for all components
    ...
  /Components/     # Data-only component definitions (Position, Health, etc.)
  /Entities/       # Entity, EntityId, EntityManager
  /Systems/        # System interfaces and implementations
/Server/           # .NET authoritative server
/Client/           # Unity client (uses Shared.dll)
```

## 🎮 Architectural Principles

- **Entity-Component-System (ECS):**
  - Entities are unique IDs (no logic or data themselves)
  - Components are pure data (no logic)
  - Systems contain all logic and operate on entities with specific components
- **SOLID Principles:**
  - Single Responsibility: Components = data, Systems = logic
  - Open/Closed: Add new features by composing new components/systems
  - Inversion of Control: Systems and managers are injected or resolved, not hardwired
- **Shared Logic:**
  - All gameplay rules, state, and serialization live in /Shared
  - Server and client both use the same ECS code for consistency

## 🔄 Replication & Networking

- **Server:**
  - Maintains the authoritative ECS world
  - Serializes and broadcasts snapshots of all replicable entities/components
  - Receives and validates client intents (input, actions)
- **Client:**
  - Receives world snapshots, reconstructs local ECS world
  - Renders entities using Unity GameObjects
  - Sends player intents (movement, actions) to server

## 📦 Example: Adding a New Component

1. Define a new data-only struct/class in `/Shared/Components/`:
   ```csharp
   public class VelocityComponent : IComponent
   {
       public Vector3 Value;
   }
   ```
2. Systems can now query for entities with `VelocityComponent` and update them.

## 🧩 Why This Design?

- **Scalability:** Easily supports hundreds of entities and flexible game rules
- **Testability:** Core logic is decoupled from Unity and can be unit tested
- **Maintainability:** New features = new components/systems, not rewrites
- **Consistency:** Server and client always agree on game rules and state

## 🚀 Next Steps

- Implement core components (Position, Health, etc.)
- Build basic systems (Movement, Combat, Respawn)
- Set up serialization for network replication
- Integrate with LiteNetLib (server) and Unity (client)
- Add scene loading from JSON for initial world state

---

*This architecture is designed for rapid prototyping and robust multiplayer gameplay, following industry best practices for modern game development.*
