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

## 🧪 Testing & Code Coverage

This project uses xUnit for unit tests. You can run tests and generate a code coverage report to ensure code quality.

### Prerequisites

You'll need the `reportgenerator` global tool. Install it with:
```shell
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### Running Tests

To run all unit tests from the command line, use the standard `dotnet test` command:
```shell
dotnet test
```

### Generating a Coverage Report

A helper script is provided to run tests, collect coverage data, and generate a detailed HTML report.

To run the script, execute:
```shell
./tools/coverage.sh
```

This will:
1. Run all tests and collect coverage data.
2. Generate an HTML report in the `coveragereport/` directory.
3. Print a link to the local `index.html` file, which you can open in a browser to view the report.


## 🚀 Next Steps

- Implement core components (Position, Health, etc.)
- Build basic systems (Movement, Combat, Respawn)
- Set up serialization for network replication
- Integrate with LiteNetLib (server) and Unity (client)
- Add scene loading from JSON for initial world state

---

*This architecture is designed for rapid prototyping and robust multiplayer gameplay, following industry best practices for modern game development.*

## 🧬 Component ID Generation

This project uses a code generation step to create a static mapping of component types to unique integer IDs. This is essential for efficient network serialization, as it allows us to send a small ID instead of a long type name.

### When to Run the Generator

You **must** run the component ID generator whenever you:
- Add a new `IComponent` type.
- Rename an existing `IComponent` type.
- Remove an `IComponent` type.

Failure to do so will result in serialization errors and mismatches between the client and server.

### How to Run the Generator

1.  **Build the Solution**: The generator needs to inspect the latest compiled assemblies. Make sure you have recently built the `Shared` and `Server` projects.
    ```shell
    dotnet build
    ```
2.  **Run the Tool**: Execute the generator tool from the root of the repository:
    ```shell
    dotnet run --project tools/ComponentIdGenerator
    ```

This will overwrite the `Shared/ECS/Replication/ComponentTypeRegistry.Generated.cs` file with the updated mapping. It is safe to commit this generated file to version control.

## 🎯 Unity Client: Refresh Shared.dll after changes

After modifying code in `/Shared`, rebuild and copy the artifacts into the Unity client so it picks up the latest logic.

```shell
# From repo root
# 1) Build
dotnet build

# 2) Regenerate component IDs (if you've added/renamed/removed components)
dotnet run --project tools/ComponentIdGenerator

# 3) Build again to ensure the generated map is included
dotnet build

# 4) Copy the Shared.dll and PDB into the Unity project's Assets folder
cp ./Shared/bin/Debug/netstandard2.1/Shared.dll ./Client/Assets/
cp ./Shared/bin/Debug/netstandard2.1/Shared.pdb ./Client/Assets/
```

Notes:
- If Unity is open, it will auto-reimport the updated DLL. If it doesn’t, re-focus the Unity Editor or force a reimport.
- Adjust `Debug`/`Release` paths as needed depending on your build configuration.
