# Multiplayer Prototype: ECS Architecture Overview

## Project Structure

```
/Shared/           # Shared ECS logic (used by both server and client)
  /Damage/         # DamageSystem, HealthSystem (regen), DeathSystem, RespawnSystem
  /ECS/
    /Components/   # Commonly used components that don't fit into a specific feature
    /Entities/     # Entity, EntityId, EntityRegistry
    /Simulation/   # World, WorldBuilder, Tick scheduling attributes
    /TickSync/     # Tick synchronization between client and server
  /Networking/     # Messaging, client/server abstractions, LiteNetLib adapters
  /Physics/        # AABB, CollisionSystem (O(n^2)), UnitCollisionSystem, velocity integration
  /Prediction/     # PredictedComponent<T> wrapper & helpers
  /Replication/    # Delta serialization, component type registry
  /Settings/       # Gameplay settings (Player, Bot, Projectile, Simulation) and SettingsMessage
/Server/           # .NET authoritative server (headless)
  /AI/             # BotAiSystem (retreat, attack, basic steering)
  /Player/         # Server-side input, spawn handlers & shot validation
  /Scenes/         # Scene JSON + loader
  appsettings.json # Runtime configuration copied next to the executable
/Client/           # Unity client (consumes Shared.dll)
  /Assets/Scripts/Core      # Core client business logic (prediction, input, ECS client systems)
  /Assets/Scripts/Adapters  # Bootstrapping/DI, presentation & rendering glue to Unity
  /Assets/Resources         # Prefabs and UI
/tests/            # xUnit tests for Shared and Server
/tools/            # ComponentId generator and coverage script
```

## Architectural Principles

- **Entity-Component-System (ECS)**
  - Entities are IDs; components are pure data; systems implement logic
- **Shared-first logic**
  - Simulation rules, replication, and serialization live under `/Shared` and are used by both server and client
- **Authoritative server**
  - Server drives simulation and replication; clients predict & reconcile

## Replication & Networking

- Server produces `WorldDeltaMessage` snapshots/deltas and broadcasts via LiteNetLib
- Client receives deltas and rebuilds local ECS state, rendering with Unity GameObjects
- Prediction via `PredictedComponent<T>` + reconciliation
- On connect, server sends `ConnectedMessage` with initial snapshot and `SettingsMessage`

## Prerequisites

- .NET 8 SDK
- Unity (LTS recommended)
- reportgenerator (for coverage)
  ```shell
  dotnet tool install --global dotnet-reportgenerator-globaltool
  ```

## Build & Run (Server)

- Configuration file: `Server/appsettings.json`
  - For portable launches, copy next to the server executable. Add this to `Server.csproj` if not present:
    ```xml
    <ItemGroup>
      <None Update="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
    </ItemGroup>
    ```
- Load configuration from the executable directory (see `Program.cs`):
  - Use `AppContext.BaseDirectory` as the configuration base path and `appsettings.json` filename.
- Run:
  ```shell
  dotnet run --project Server
  ```

## Unity Client: Workflow

- Build `Shared` and copy artifacts into Unity after Shared changes:
  ```shell
  # From repo root
  dotnet build
  dotnet run --project tools/ComponentIdGenerator
  dotnet build
  cp ./Shared/bin/Debug/netstandard2.1/Shared.dll ./Client/Assets/
  cp ./Shared/bin/Debug/netstandard2.1/Shared.pdb ./Client/Assets/
  ```
- Launch Unity and press Play; the client connects to the server, consumes replication, and renders entities.

## Testing & Code Coverage

- Run tests:
  ```shell
  dotnet test
  ```
- Coverage report:
  ```shell
  ./tools/coverage.sh
  ```
  The report is generated in `coveragereport/` and can be opened in a browser.

### Code Coverage Overview

- A sample coverage dashboard is available at:
  
  ![Code Coverage](docs/Code%20Coverage.jpg)
  
- Critical paths covered by current tests:
  - **ECS Core**: `Entity`, `EntityRegistry`, entity lifecycle events (create/modify/remove)
  - **Physics**: `VelocitySystem`, `WorldAABBUpdateSystem`, `CollisionSystem`, `UnitCollisionSystem`
  - **Damage & Lifecycle**:
    - `DamageSystem` (damage application, projectile destruction)
    - `HealthSystem` (regen behavior and capping at MaxHealth)
    - `DeathSystem` (death detection and record creation)
    - `RespawnSystem` (timed respawn for players/bots)
  - **Replication**: `BinaryComponentSerializer`, `WorldDeltaMessage`, `EntityDelta`, `ClientReplicationSystem`, integration tests
  - **Prediction & Tick Sync**: `PredictedComponentExtensions`, `ClientTickSystem`
  - **Messaging**: message construction/serialization (Connected, Delta, PlayerShot/Movement)

> CI note: On deploy, GitHub Actions runs unit tests and uploads the HTML coverage report from `coveragereport/` as a build artifact.

## Component ID Generation

This project uses a code generation step to map components to compact integer IDs for network serialization.

- Run whenever adding/renaming/removing an `IComponent`:
  ```shell
  dotnet build
  dotnet run --project tools/ComponentIdGenerator
  dotnet build
  ```

## Key Systems Overview

- **Physics**
  - `WorldAABBUpdateSystem`: builds world-space AABBs from position/rotation/local bounds
  - `CollisionSystem`: naïve O(n^2) broad/naïve narrow phase for intersections
  - `UnitCollisionSystem`: separates overlapping units; ignores entities with `DoesNotOccupySpaceTagComponent`
  - `VelocitySystem`: integrates velocity using `SimulationSettings.FixedDeltaTime`
- **Damage & Lifecycle**
  - `DamageSystem`: applies damage on collision; destroys projectiles on hit
  - `HealthSystem`: health regeneration (default +5 per run) capped at MaxHealth
  - `DeathSystem`: converts zero-HP entities into `RespawnComponent` records and destroys originals
  - `RespawnSystem`: respawns bots/players when `RespawnAtTick` is reached
- **AI**
  - `BotAiSystem`: retreat/attack behavior, faces movement direction and target

---

## Roadmap: Toward a Globally Distributed Real‑Time Multiplayer Mobile Game

A pragmatic plan of technical work to evolve this prototype into a planet‑scale, low‑latency mobile title.

### 1) Netcode & Simulation
- **Entity thread safety and performance**: Systems should be given a safely mutable set of entities, and access to an efficient query system. Currently we use many iterations over list copies. We guarantee
thread safety by running the world simulation on a single thread, but we are also using event handlers
that are not on the same thread (should be easy to create an event handler that pipes callbacks to the
same thread as the world simulation).
- **Interest management**: per‑client relevance filtering, spatial partitioning (grids/quadtrees) to cut bandwidth.
- **Delta & compression**: component‑level change tracking, bit‑packing, dictionary compression; snapshot interpolation.
- **Lag compensation**: server‑side rewind for hitscan/projectiles to address mobile/geo latency.
- **Prediction/reconciliation improvements**: per‑component policies, client drift detection.

### 2) Matchmaking Logic

**Lobby / Matchmaking**: Add a regional matchmaking system to group up players based on region preference
or predefined group code
- **Session management**: admission control, rejoin, migration on node failure.

### 3) Transport, Routing and Infrastructure
- **World Delta improvements**: Send the deltas via unreliable channel. Use a cursor system
so clients can ack the last tick received.
- **Region routing**: geo‑DNS/Anycast to nearest edge; region data centers with automatic failover.
- **Containerized game servers**: immutable images, config via env/secrets; blue/green & canary deploys.

### 4) Data & Persistence
- **Authoritative storage**: player account, inventory, MMR, cosmetics; cloud K/V + RDBMS for transactions.
- **State snapshots**: crash‑safe checkpoints for long‑lived sessions; deterministic replays for debugging.

### 5) Security & Integrity
- **Authentication**: Third party authentication (Apple / Google, etc..)
- **Anti‑cheat Reporting**: Repeated server validation reporting, with rate limiting and ip ban.

### 6) Observability & SRE
- **Metrics**: p50/p95/p99 RTT, server tick time, backlog, packet loss, churn; per‑region SLOs.
- **Tracing & logging**: structured logs, distributed traces across gateway → game server → storage.
- **Dashboards & alerting**: capacity, errors, hot shards; on‑call runbooks and game‑specific health checks.

### 7) Mobile Readiness
- **Adaptive networking**: dynamic tick rate, LOD of state, tolerant to backgrounding and packet loss.
- **Connectivity resilience**: seamless roaming (Wi‑Fi ↔ LTE), auto‑reconnect
- **Perf & battery**: CPU/GPU frame budgets, GC minimization, asset streaming, shader variants.

### 8) Developer Experience
- **CI/CD**: Deploy to game host Github Actions.
- **Load testing**: headless bot swarm + chaos (packet loss, latency, disconnect storms).

### 9) Content & Live Ops
- **Config as data**: live‑tunable gameplay settings, rollout with staged percentages & region gates.
- **Seasonal systems**: battle pass, events, challenges.
- **Store & payments**: platform entitlements, fraud prevention, purchase receipts validation.

### 10) Risk & Compliance
- **Privacy & data residency**: regional storage controls, GDPR/CCPA tooling.
- **Age gating & chat safety**: moderation, filtering, reporting pipelines.

---

## Notes & Tips
- Run the component ID generator whenever you add/rename/remove components (see above).
- Ensure `appsettings.json` is copied next to the server executable; configure `Program.cs` to read from `AppContext.BaseDirectory` so launch directory doesn’t matter.
- Keep Unity’s `Shared.dll` in sync after every change under `/Shared`.
