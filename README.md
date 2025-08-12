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
- Restore Unity NuGet packages (first time or after dependency changes):
  - Open the Unity project in `Client/`
  - In the Unity Editor menu, go to `NuGet` → `Restore` (NuGetForUnity)
  - Wait for packages from `Client/Assets/packages.config` to be restored and imported
- Launch Unity and press Play; then:
  - Click **Start Local** to connect to your locally running server.
  - Click **Start Remote** to connect to the default remote (`multiplayer-prototype.fly.dev`). No manual address changes needed.

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

## How to Play

You can play a live version of the game in your browser on Unity Play:

**[️Play Online Now](https://play.unity.com/en/games/c6bef702-ce4b-4eb2-847a-cd47b182e7d6/ecs-multiplayer-prototype)**

The browser client connects to the latest version of the server deployed on Fly.io.

---

### 1. Playing Locally

1. **Run the Server**: Open a terminal at the repository root and run the server:
   ```shell
   dotnet run --project Server
   ```
   The server will start and listen on `0.0.0.0:9050` by default.

2. **Run the Client**:
   - Open the `Client` project in the Unity Editor.
   - Press the **Play** button in the editor.
   - Click **Start Local** to connect to your local server (no manual address changes required).

### 2. Playing Remotely (via Fly.io)

1. **Deploy the Server**: Make sure you have deployed the server to Fly.io by following the deployment steps below.

2. **Configure the Client**:
    - In the Unity Editor, select the `Client/Assets/Scripts/Adapters/Settings/GameSettings.asset` file.
    - In the Inspector, change the `Server Address` to your Fly.io app's hostname (e.g.,`multiplayer-prototype.fly.dev`).
    - Press the **Start Local** button on game start.

3. **Run the Client**:
    - Press the **Start Remote** button on game start.

## Deploying to Fly.io (Docker)

This project is configured for deployment to [Fly.io](https://fly.io), a modern PaaS that supports containerized applications with dedicated
UDP ports.

### Prerequisites

- [Docker](https://www.docker.com/products/docker-desktop/) installed and running.
- The [Fly.io CLI (`flyctl`)](https://fly.io/docs/hands-on/install-flyctl/) installed and authenticated (`flyctl auth login`).

### How It Works

- **`fly.toml`**: This configuration file (in `deploy/fly.toml`) tells Fly.io how to build and run the application. It defines two services:
    1. A **TCP service** on ports 8080/443 for HTTP/S traffic. This is used by Fly.io for health checks.
    2. A **UDP service** on port 9050 (by default) for the game server itself.
- **`Server/Health/HttpHealthServer.cs`**: A minimal, built-in TCP server that responds to Fly.io's health checks at port 8080.
- **`Dockerfile`**: The Dockerfile at `deploy/Dockerfile` builds the server. It's configured to work on both `amd64` (standard cloud
  servers) and `arm64` (Apple Silicon) architectures for development and deployment.

### Deployment Steps

1. **Launch the App (First-Time Deploy)**
    - Navigate to the repository root in your terminal.
    - Run `fly launch --path deploy/fly.toml`. This command will:
        - Read the configuration from the specified path.
        - Prompt you to choose an app name (e.g., `ecs-multiplayer-prototype`) and an organization.
    - You do not need to set up a Postgres database or deploy immediately when prompted.

2. **Deploy**
    - Once the app is launched, deploy it by running:
      ```shell
      fly deploy -a multiplayer-prototype -c deploy/fly.toml
      ```
    - `flyctl` will build the Docker image using the settings in `deploy/fly.toml`, push it to Fly.io's registry, and provision a virtual
      machine to run the server.

3. **Connect Your Client**
    - After deployment, your game server will be available at `<your-app-name>.fly.dev` on the UDP port defined in `fly.toml` (e.g., 9050).
    - Update your Unity client's `NetworkSettings` to point to this address and port to connect.

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

- **Entity thread safety and performance**: Systems should be given a safely mutable set of entities, and access to an efficient query
  system. Currently we use many iterations over list copies. We guarantee
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

## Debugging Tools

- Bandwidth/Network Stats Overlay (Client)
  - In Play Mode, press `F3` to toggle the in-game bandwidth/packet stats overlay.
  - Useful to validate message rates, snapshot sizes, and client RTT.

- ECS Inspector (Unity Editor)
  - In the Unity Editor (Play Mode), open: `Debug` → `ECS Inspector`.
  - Browse entities, components, and systems live while the simulation runs.

## Notes & Tips

- Run the component ID generator whenever you add/rename/remove components (see above).
- Ensure `appsettings.json` is copied next to the server executable; configure `Program.cs` to read from `AppContext.BaseDirectory` so
  launch directory doesn’t matter.
- Keep Unity’s `Shared.dll` in sync after every change under `/Shared`.
