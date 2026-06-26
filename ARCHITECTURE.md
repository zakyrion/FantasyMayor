---
category: C
read: always
tags: [architecture, ecs, conventions]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ECS_REFERENCE](ECS_REFERENCE.md)"
  - "[GAMEPLAY_FOUNDATION](GAMEPLAY_FOUNDATION.md)"
---

# FantasyMayor — Architecture Reference

> Doc map & read-priority: see `INDEX.md` (generated; lists every doc and when to read it).

## Stack
- Engine: Unity
- ECS: `DefaultEcs` with a DoD style, not Unity DOTS
- DI: `VContainer`
- Async: `UniTask`
- Asset loading: Unity `Addressables`
- Input: Unity `InputSystem`
- Rendering: Universal Render Pipeline
- UI: Unity `UI Toolkit` (UXML/USS); `Unity App UI` (`com.unity.dt.app-ui`) is the component foundation — see `GENERAL_UI_STYLE.md` §15

## Repository Map

```text
FantasyMayor/
├─ Assets/
│  ├─ Domains/                # Game-rule bounded contexts (Map, Economy, Actors) — one asmdef each
│  ├─ Presentation/           # Render/view layer (terrain, resources, icons) — one asmdef
│  ├─ Modules/                # Engine-facing / infra / UI feature modules
│  ├─ Scripts/                # Shared runtime primitives and app-root installers
│  ├─ Addressables/           # Authored addressable content and config assets
│  ├─ AddressableAssetsData/  # Addressables editor configuration
│  ├─ Scenes/                 # Unity scenes and sub-scenes
│  ├─ Prefabs/                # Global prefabs
│  ├─ Resources/              # Legacy/global resources
│  ├─ Materials/
│  ├─ Shader/
│  ├─ Plugins/                # Odin, Easy Save, Dreamteck, Animancer
│  ├─ Packages/               # Vendored package payloads under Assets
│  ├─ Settings/
│  ├─ Editor/
│  ├─ TextMesh Pro/
│  ├─ Schemes/
│  └─ TutorialInfo/
├─ Packages/                  # Unity package manifest and lock file
├─ CLAUDE.md                  # Agent process rules
├─ ARCHITECTURE.md            # This file — project-wide architecture policy
├─ ECS_REFERENCE.md           # Central entity / world-component / event registry
├─ SYSTEMTEMPLATE.md          # Template catalog for new systems
├─ CONFIGTEMPLATE.md          # Template catalog for config flows
├─ DOC_STANDARD.md            # How every MD file is written
├─ GAMEPLAY_FOUNDATION.md     # GD doc — target gameplay cycles
└─ GENERAL_UI_STYLE.md        # UI design language (read fully only for UI work)
```

## Source Of Truth
| What | Where |
|---|---|
| Feature and domain runtime code | `Assets/Modules/*` |
| Shared utility primitives (`Box<T>`, `Result<T>`, `FrameBox<T>`, `StateAllowedAttribute`) | `Assets/Scripts/Core` |
| Shared ECS base systems and loop contracts | `Assets/Scripts/DefaultECSExtensions` |
| App-root DI composition | `Assets/Scripts/Installers/*` |
| Authored config assets | `Assets/Addressables/Configs/*` |
| Scenes | `Assets/Scenes/*` |
| Package dependencies | `Packages/manifest.json` |

## Current Scene And Content Layout
- Main scenes: `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/TerrainMask.unity`
- Sub-scene: `Assets/Scenes/SampleScene/GameplaySubScene.unity`
- Addressable config roots:
  - `Assets/Addressables/Configs/TerrainGenerationConfig`
  - `Assets/Addressables/Configs/TerrainViewConfigs`
  - `Assets/Addressables/Configs/UserInput`
- Additional addressable content: `Assets/Addressables/Textures`
- Addressables editor data is stored in `Assets/AddressableAssetsData/*`

## Shared Runtime Assemblies

| Assembly | Path | Responsibility |
|---|---|---|
| `Core` | `Assets/Scripts/Core` | Shared utility primitives: `Box<T>`, `Result<T>`, `FrameBox<T>` (frame-bounded state), `StateAllowedAttribute` (deliberate statefulness marker), disposal helpers, status primitives |
| `DefaultECS.Extensions` | `Assets/Scripts/DefaultECSExtensions` | ECS loop contracts and base systems: `IUpdatedSystem`/`UpdatedSystem`, `ILateUpdatedSystem`/`LateUpdatedSystem`, `ConfigLoaderSystem`, `IUniTaskSystem<T>`, `IPrioritizedUniTaskSystem<T>`, `UniTaskSequentialSystem<T>`, `EventCleanupSystem`, `EventTag`, `GameState` |
| `Installers` | `Assets/Scripts/Installers/Addressable` | App-root `IInstaller` implementations such as `AddressableInstaller` |
| `Installers.World` | `Assets/Scripts/Installers/World` | Root `LifetimeScope`, world composition, input wiring, installer orchestration |

## Current Modules

Modules are now **engine-facing / infra / UI only**. The former Hex/Terrain feature modules became the
`Map` rule-domain and the `Presentation` layer (see below).

| Module | Assemblies | Current responsibility |
|---|---|---|
| `Addressable` | `Addressables.Core`, `Addressables.Implementations` | Public addressable loading contract and implementation |
| `AxialSystem` | `AxialSystem` | Axial hex math, coordinates, generic axial grid primitives (shared kernel; domains may depend on it) |
| `Boot` | `Boot.Core`, `Boot.Implementation` | Boot phase markers (incl. `MapGenerationStep`), boot MonoBehaviour, and the hand-wired `GameModeMachine` (per-state system composition) |
| `Cameras` | `Cameras` | Owns the active scene camera as a world component (`CameraComponent`), decoupled from consumers |
| `Configs` | no dedicated asmdef | Generic config provider abstractions used by runtime code |
| `CurveBuilders` | `CurveBuilders` | Shared curve builder contract |
| `MainUI` | `MainUI` | Main UI module (per-window subfolders): generator menu (MainMenu state), selection-driven hex info panel (Gameplay), End Turn button (Gameplay HUD) |
| `MainCanvas` | `MainCanvas.Core`, `MainCanvas.Implementation` | Root canvas abstraction and provider implementation; DI registration lives in `WorldInstaller` |
| `UserInput` | `UserInput` | Camera and player input ECS bridge plus camera movement config flow |
| `Turn` | `Turn` | Turn-phase orchestration engine: on a turn pulse runs ordered phase subsystems off the main thread and signals "turn in progress" via a world component. SCAFFOLD — no phases yet |

## Current Domains (`Assets/Domains/<Name>/`, one asmdef each)

Game-rule bounded contexts — pure data + logic, with **no view/render dependency**.

| Domain | Assembly | Current responsibility |
|---|---|---|
| `Map` | `Domains.Map` | The world-map bounded context: hex grid + terrain types (`Hex/`), procedural map generation (`Generation/` — `MapGenerationSystem` + Mountain/River/Lake/Sea subsystems, on `MapGenerationStep`), natural per-hex resources (`HexResources/`: Forest/Clay/Fish), hex pathfinding (`Pathfinding/`) |
| `Economy` | `Domains.Economy` | Owner-agnostic economic substrate: inventory resource types + the generic `ResourceLoadoutSpawner` mechanism, district scaffold; reads `Domains.Map` (hex types). No actor dependency |
| `Actors` | `Domains.Actors` | Actor identities (City, Mayor) + startup composition: per-actor spawn, Mayor config/loader, resource loadout; reads `Domains.Economy` |
| `Actions` | `Domains.Actions` | Application/orchestration layer: actor verbs + cross-domain turn processing; reads `Domains.Economy` + `Domains.Actors`. SCAFFOLD — no systems yet |

## Presentation Layer (`Assets/Presentation/`, one asmdef `Presentation`)

The entire render/view layer, consolidated into one assembly. Depends one-way on `Domains.Map`
(plus `Cameras`, `CurveBuilders`); **domains never depend on it**.

| Sub-area | Former module | Responsibility |
|---|---|---|
| `Terrain/` | `Terrain.View` | Terrain mesh, textures, water view, isolines, smoothing, runtime view systems |
| `HexResources/` | `HexResourcesView` | Resource visualization (forest/clay/fish views, ground painting) |
| `HexIcons/` | `HexIcons` | Screen-space per-hex icon overlay |

## Module Layout Rules

Every new feature should still prefer `Assets/Modules/<FeatureName>/` and use this target layout:

```text
<FeatureName>/
├─ Components/   # Pure data structs
├─ Tags/         # tag components for entities
├─ Events/       # one-frame event components for systems
├─ Configs/      # ScriptableObject class definitions
├─ Data/         # Collections, records, enums, helper types
├─ Systems/      # ECS systems and runtime orchestration
├─ Helpers/      # Stateless computation helpers used by systems
├─ Views/        # MonoBehaviour view layer
├─ Prefabs/      # Module-scoped prefabs, uxml, uss
└─ Installer/    # Feature-specific VContainer registration
```

Placement rules:
- New domain code goes to `Assets/Modules/<FeatureName>/`
- Feature-specific DI goes to `Assets/Modules/<FeatureName>/Installer/`
- Cross-cutting or app-root DI goes to `Assets/Scripts/Installers/`
- UI assets go to `Assets/Modules/<FeatureName>/Prefabs/`

## DI Composition Rules

- `WorldInstaller` is the only `LifetimeScope` in the project.
- App-root composition happens in `WorldInstaller.Configure()`.
- Module installer classes should implement `VContainer.IInstaller`.
- Use a plain class for an installer when it has no serialized data.
- Use `MonoBehaviour + IInstaller` only when an installer must own `[SerializeField]` data.
- Installer execution order is explicit in `WorldInstaller.Configure()`; dependencies must be installed before dependents.
- Per-frame systems are registered with their **concrete** type (`.As<TheSystem>()`), not as
  `IUpdatedSystem`/`ILateUpdatedSystem` — `Boot` injects concretes and wires them into game states by hand.

Folder contract:
| Folder | Never contains |
|---|---|
| `Components/` | Logic or side effects |
| `Configs/` | Runtime logic or config assets |
| `Data/` | ECS systems or MonoBehaviours |
| `Systems/` | View logic or config definitions |
| `Helpers/` | Cross-frame state or entity ownership |
| `Views/` | Business logic |
| `Installer/` | Anything except DI registration |

## Current Structure Notes And Exceptions
- The repository is partially standardized, not fully uniform.
- `TerrainGenerator` and `TerrainView` are the closest matches to the full module template.
- `AxialSystem`, `CurveBuilders`, and `Configs` are flatter utility-style modules rather than full feature modules.
- `Boot`, `Addressable`, and `MainCanvas` use explicit contract and implementation splits.
- `Pathfinding` has a root runtime assembly plus a separate installer assembly that exposes an `IInstaller`.
- `UserInput` currently has `Components`, `Configs`, and `Systems`, but no module-local installer folder.
- `Assets/Scripts/Extentions` is a legacy typo-named folder and should be treated as existing structure, not a naming standard.

## Domains, Presentation & Modules (architecture)
Three top-level code layers, boundary enforced by asmdef references:

- **Domain** (`Assets/Domains/<Name>/`) = a game-rule **bounded context** (DDD-strategic): owns its
  entity tables and turn-phase logic. Pure data + logic, **free of any view/render dependency**.
  Current: `Map`, `Economy`, `Actors`, `Actions`. They form the DAG **substrate → agents → verbs**:
  `Map`/`Economy` (leaves) → `Actors` (agents) → `Actions` (verbs).
- **Presentation** (`Assets/Presentation/`) = the consolidated **render/view layer** (one assembly):
  terrain/resource/icon views. Depends one-way on the domains it renders; **domains never depend on it**.
- **Module** (`Assets/Modules/`) = engine-facing infrastructure & UI plumbing: addressables, input,
  cameras, canvas, boot, config providers, shared kernels (`AxialSystem`, `CurveBuilders`), UI windows.

**Why this shape.** This is **DDD-strategic bounded contexts + a layered presentation tier**, on top of a
**DoD/ECS** data substrate. DDD (Evans) deliberately isolates the domain model from UI/infra, so pulling
all views into one layer is *pro*-DDD, not vertical-slice. DoD's "no hierarchy" is about data/types
(flat tables, composition, no inheritance — see the Table Rule); it is orthogonal to this code-layering.
We borrow DDD's **strategic** half (contexts, ubiquitous language, layering), not its OO **tactical**
patterns (aggregates/repositories), which ECS expresses as tables + systems.

- One asmdef per domain and one for presentation (feature subfolders inside; namespaces follow:
  `Domains.Map.Hex.*`, `Presentation.Terrain.*`, …).
- Cross-domain dependencies are expected (e.g. `Economy → Map` for hex types, `Actors → Economy` for the
  resource substrate, `Actions → {Economy, Actors}`) and MUST be declared in `ECS_REFERENCE.md`
  (Cross-Module Component Reads). Direction follows the substrate→agents→verbs DAG — owner-keyed logic
  lives in `Actors`/`Actions`, never in the owner-agnostic `Economy` substrate.
- The Module Layout Rules above apply to all three layers (each feature/sub-area keeps the
  `Components`/`Systems`/… split).

## Public Contract Split
- When a module exposes a reusable public API, prefer a `Core/` contract assembly plus an `Implementation/` assembly.
- Current canonical examples are `Addressable`, `MainCanvas`, and `Boot`.

---

## System Taxonomy

Canonical vocabulary. Every system in the project plays exactly ONE of these roles. Use these names
in docs, reviews, and design discussions. `SYSTEMTEMPLATE.md` carries the concrete template for each.

| Role | Base type | Driven by | Lifecycle |
|---|---|---|---|
| **Config Loader** | `ConfigLoaderSystem` (`IUniTaskSystem<ConfigLoadStep>`) | Boot bootstrap, once at startup | one-shot; `MarkAsLoaded()` guards re-entry |
| **Pipeline Stage** | `IPrioritizedUniTaskSystem<MapGenerationStep>` | `MapCreation` state, sequential, ascending `Priority` | one-shot async |
| **Pipeline Orchestrator** | a Pipeline Stage that fans out into SubSystems | `MapCreation` state | one-shot; NO domain logic of its own |
| **Pipeline SubSystem** | per-orchestrator abstract base (async `ViewSubSystem : IUniTaskSystem<GameState>` or sync `HexResourcesViewSubSystem : ISystem<GameState>`) | its orchestrator, ascending `Priority`, `IsEnabled` honored | one-shot |
| **Per-frame System** | `UpdatedSystem` / `LateUpdatedSystem` (`AEntitySetSystem<GameState>`) | the ACTIVE game state only | every frame; must justify why it cannot be reactive |
| **Reactive System** | `UpdatedSystem` whose base set is `With<SomeEvent>` | a one-frame pulse; zero idle cost | pulse → reconcile against current state, idempotent |
| **Cleanup** | `EventCleanupSystem`, `Priority = int.MaxValue` | every active state, runs last | disposes all `EventTag` entities each tick |

**Apply via:** choosing a role for a new system, and the per-role skeleton + mechanics (orchestrator
sorting, subsystem query ownership, pulse + reconcile shape) → `SYSTEMTEMPLATE.md` → Step 0 and
Templates 1–4; Config Loader → `CONFIGTEMPLATE.md`. This file owns the *taxonomy and invariants*; the
templates own the *procedure*.

Role invariants (policy — hold regardless of the template you follow):
- One-frame events DO NOT survive the async `MapCreation` pipeline (`EventCleanupSystem` disposes them
  at end of tick) — startup bulk work is always a Pipeline Stage/SubSystem, never event-driven.
- A **Reactive System** is the DEFAULT for runtime logic. A **Per-frame System** must justify in
  writing why it cannot be reactive; no justification = a decomposition smell (see Decomposition Rules).
- An **Orchestrator** contains no domain logic of its own; a **SubSystem** exists only under an
  orchestrator.
- Naming: orchestrators and stages are named `…System`, subsystems `…SubSystem`. Reactive systems carry
  intent names (`ForestSpawnSystem`, `HexIconsVisibilitySystem`) — there is no mandated
  `…ReactiveSystem` suffix.

**Turn pipeline (module `Turn`) — same roles, different scope.** The Orchestrator/SubSystem roles are
reused for turn processing, but turn-scoped (re-run every turn on a `NextTurnEvent` pulse) and executed
OFF the main thread (`UniTask.RunOnThreadPool`), unlike the one-shot, main-thread `MapCreation`
pipeline. The phase base is `TurnPhaseSubSystem`; the launcher is the per-frame `TurnProcessorSystem`
(it polls the in-flight run each frame, so it is justified as a Per-frame System, not reactive). Every
world write stays on the main thread; the pool only computes. SCAFFOLD — zero phases today.

## State Storage Taxonomy

Four storages. Pick by answering: how many instances, and does anything need to FIND it via an
entity query?

**Apply via:** a loaded config is always a world component — the config-loader procedure that does it
is `CONFIGTEMPLATE.md` → STORAGE RULE.

| Storage | Use when | Access | Registry |
|---|---|---|---|
| **Entity table** | N rows of the same shape (hexes, resources, views, icon containers) | query = key + discriminator (Table Rule); `EntitySet` / `EntityMap` / `EntityMultiMap` | `ECS_REFERENCE.md` Entity Registry |
| **World component** | exactly ONE instance, and NO consumer needs it in an entity query | `world.Set` / `world.Get`, guarded by `world.Has` | `ECS_REFERENCE.md` World Component Registry |
| **One-frame event entity** | a signal that something changed; consumed by a Reactive System this same frame | marker component + `EventTag`; `EventCleanupSystem` disposes at end of tick | `ECS_REFERENCE.md` Event Registry |
| **Singleton entity** | exactly ONE instance, but it MUST appear in entity queries (a per-frame system anchors on it, or reactive filters watch it) | `With<TheComponent>` set with `Count`-guard | `ECS_REFERENCE.md` Entity Registry |

World component contract:
- A world component is **not an entity**: it never appears in `world.GetEntities()` and cannot be
  matched by `With<T>` / `WhenAdded<T>` / `WhenChanged<T>`. If its change must drive reactive
  consumers, raise an explicit one-frame event entity alongside the `world.Set`.
- All loaded configs are world components (`CONFIGTEMPLATE.md`). Runtime singletons follow the same
  storage: `CameraComponent`, `VertexGridComponent`, `TerrainTextureComponent`,
  `HexIconsViewComponent`, `HexIconsVisibilityComponent`.
- A world component carrying a **reference type** (`VertexGrid`, `Texture2D`) is set ONCE at
  creation; the payload object is then mutated in place by its writers. `world.Set` is never
  re-called after a mutation — readers always see the live object via `world.Get`.
- Singleton entities are the exception, not the default. Current ones exist because systems anchor
  per-frame ticks on them or query them: `TerrainViewComponent`, `HexSelectedComponent`,
  `WaterViewComponent`, `HexSelectionViewComponent`, `HexInfoPanelViewComponent`,
  `PlayerInputComponent`. When adding new single-instance state, default to a world component;
  create a singleton entity only when an entity-query consumer exists from day one.

## Decomposition Rules

The project moved from per-frame "god-systems" to decomposed role-pure systems. Apply these rules
when designing or reviewing any system.

**Apply via:** the reactive-split skeleton and the point-of-use "split when" checklist are in
`SYSTEMTEMPLATE.md` → Template 1 and its Responsibility warnings.

**God-system smell — split it when a system:**
- both **creates and destroys** the same kind of content;
- **diffs world state every frame** to find out "what changed" (multi-filter scans, sorts,
  per-frame set comparisons);
- holds **more than two unrelated query families** (it serves several masters);
- runs in **multiple game states** for different reasons.

**The split recipe** (worked example: `ForestViewSyncSystem` → 3 systems):
1. The startup bulk becomes a **Pipeline SubSystem** (one-shot, runs once inside `MapCreation`):
   `ForestHexResourceViewSubSystem` plants every forest hex and paints ground once.
2. Each runtime responsibility becomes its own **Reactive System** in `Gameplay`:
   `ForestSpawnSystem` (on `ForestHexAppearedEvent`) and `ForestDespawnSystem`
   (on `ForestHexRemovedEvent`).
3. Shared computation moves to a stateless **helper** (`Helpers/`): `ForestPlanter`.

**Reactive = generic pulse + reconcile:**
- The event is a **payload-less one-frame pulse** ("something changed"), not a per-entity delta.
  No coordinates, no lists in the event.
- On the pulse, the system **reconciles against current world state**: build the current set, diff
  it, act on the difference. Work with state, not with transitivity.
- Reconciliation makes the system **idempotent**: a second pulse in the same frame finds nothing to
  do. Missed or coalesced pulses are harmless — the next pulse repairs everything.
- The consumer is an `UpdatedSystem` whose base set is `With<TheEvent>` → zero cost while no pulse
  exists. The pulse entity itself is ignored inside `Update`.
- Emitters may be deferred: build the reactive consumer as a dormant scaffold first, wire emitters
  when the gameplay mechanic lands.

## ECS And Runtime Conventions

- **Component naming by role (suffix):**
  - a component **carrying data** → `…Component` (e.g. `HexIdComponent`, `HexIconsVisibilityComponent`)
  - a **tag / marker** component (empty, presence-only) → `…Tag` (e.g. `HexTag`, `EventTag`)
  - a **one-frame event** component → `…Event` (e.g. `ForestHexAppearedEvent`, `SelectedHexChangedEvent`)

  Pre-existing `…EventComponent` names (e.g. `TerrainGenerationGenerateEventComponent`) predate this rule;
  they stay until a deliberate rename, but new events use the `…Event` suffix.
- Single-component entity creation may chain: `world.CreateEntity().Set(...)`
- Once more than one component is assigned, stop chaining and use a local entity variable
- Prefer instance-based design; use `static` only when a type is truly stateless utility
  infrastructure (e.g. `ForestGroundPainter`).

### Statelessness And Collections (the two hard bans)

These two bans are machine-checked by `/arch-check`. They apply to every system.

**Apply via:** the applied per-template form is in `SYSTEMTEMPLATE.md` → Global rules.

**Ban 1 — no stateful systems.** A system holds no mutable per-instance state. Instance fields must
be `readonly` handles: DI dependencies, `World`, query caches. What does NOT count as state:
- query caches (`EntitySet`, `EntityMap<T>`, `EntityMultiMap<T>`) — they are declarative,
  self-maintaining views of world state;
- `const` / `static readonly` configuration values.

What DOES count as state: any reassignable field, and any `readonly` field whose CONTENTS mutate
across frames (collections, arrays, `StringBuilder`, `Native*` buffers held between ticks).

Escape hatches, in order of preference:
1. Move the state where it belongs — onto an entity (a component) or into a world component.
2. `FrameBox<T>` (`Core`) for state valid only within a bounded number of frames (e.g. a per-frame
   cache resolved in `PreUpdate`): it is frame-stamped and fails loud on a stale read.
3. `[StateAllowed("reason")]` (`Core`) on the field — a deliberate, reviewed exception that
   `/arch-check` skips. Always pass the reason.

**The default home for "system state" is a component — reach for 2–3 only after ruling out 1.**
Most things that feel like per-system state are not: a status flag, an in-flight marker, a progress
counter, an "is this running" / "did this complete" bit, a handle to the thing currently being
processed — these are domain state that belongs ON AN ENTITY (a component) or in a WORLD component,
and moving them there breaks nothing. The system then just reads/writes that component and stays
stateless. A component is not limited to "intrinsic data" — a transient, single-instance lifecycle
flag is a perfectly valid world component. Do not assume a value must live in the system just because
only that system touches it today; the moment a value lives on a component, any future system can
observe it without auditing the writer. Worked example: `TurnProcessorComponent` (module `Turn`) is a
world component that holds the in-flight turn's status and doubles as the "turn in progress" signal;
the launching `TurnProcessorSystem` keeps zero mutable fields.

**Ban 2 — no `System.Collections.Generic` in systems.** Use `Unity.Collections`
(`NativeList`, `NativeHashSet`, `NativeParallelHashMap`, …) and dispose explicitly.
- `Allocator.Temp` for within-frame scratch; `Allocator.Persistent` (explicit dispose) when a
  container must outlive an `await` or cross a `RunOnThreadPool` boundary.
- **Exception — managed elements:** a collection whose elements are managed types (`GameObject`,
  view references, `Entity`-wrapping records) stays `System.Collections.Generic`, because a
  `NativeContainer` only holds `unmanaged` types. Mark such cases with a short comment.
- Enums can't be `NativeHashSet`/`NativeParallelHashMap` **keys** (no `IEquatable<T>`) — key on the
  underlying `int`. As a **value** an enum is fine (`unmanaged`).
- In `MonoBehaviour` (view) code, `System.Collections.Generic` is fine.

### Component Writes And Reactivity (DefaultEcs)
Project rule: **always write components through `entity.Set<T>(value)`** — the publishing write path.
This is mandatory: any component can gain a reactive consumer without auditing every place that writes it.

- **Do not** mutate through `ref entity.Get<T>()`. In-place mutation does not publish a change, silently
  breaking `WhenChanged<T>` sets and `SubscribeComponentChanged<T>` callbacks. `ref Get` is read-only.
- `ref` + `entity.NotifyChanged<T>()` is an escape hatch, **not** an approved pattern. A forgotten
  `NotifyChanged` is a silent desync; it also cannot deliver the true old value (overwritten in place).
  Use `Set(value)`.
- Reactive consumers: `WhenChanged<T>()` (batched — call `Complete()` each tick) and
  `world.SubscribeComponentChanged<T>(...)` (immediate, gives old + new).
- DefaultEcs tracks the write **call**, not the value — no built-in value-diff. `With<T>` / `Without<T>`
  filters track component presence only.
- **Entity set-membership diffs are a separate case.** Reacting to which *entities* enter or leave a set
  uses `WhenAdded` / `WhenRemoved`, not `WhenChanged`. The project's chosen alternative for view upkeep
  is the pulse + reconcile pattern (Decomposition Rules) — prefer it over `WhenAdded`/`WhenRemoved`
  buffers for new code.

### Collector And Output Methods
- A method that produces a value or fills a collection MUST report success/validity through a `bool`
  return and be named `Try…` (`TryGet`, `TryCollect`, `TryPick`). The caller branches on that `bool`.
- The caller MUST NOT infer success by inspecting the output — `== null`, `.Count == 0`, `.Length == 0`
  as a success signal is forbidden. It couples the caller to the method's internals. Branch on the `bool`.
- Make data direction visible at the call site: pass a written output as `ref` (or `out`), a read-only
  input as `in`. Never pass a collection the method **writes** by value — a by-value `NativeHashSet` /
  `NativeList` still aliases the same native memory, so the mutation is invisible at the call site.
- A producer with exactly one output **and** guaranteed success may simply **return** the value. Use the
  `Try…` + `ref`/`out` + `bool` form when success is not guaranteed, or when there are multiple outputs.
- Reference example: `ForestPlanter.TryPickForestEntry` — input as a parameter, result via `out`,
  `bool` return.
- A single-use private helper that reads class fields and just unfolds the caller's linear flow does **not**
  earn extraction — inline it into the caller. Extract only when the method is signature-complete (inputs
  as parameters) and is either reused or a self-contained operation. `Update` (a system's orchestration
  root) may read the system's own fields directly.

### Relational Modeling — Table Rule
An entity "table" is defined by its query, and a query MUST name the table, not just the key.

- **Table = key component + discriminator component.** A bare `With<KeyComponent>` query is
  **forbidden** — it is a UNION of every table sharing that key space, not a table.
- The same key component is the **primary key** on the owner table and a **foreign key** on the
  parallel tables. The hex key space (key: `HexIdComponent`) currently holds four tables:

  | Table | Discriminator | Key role |
  |---|---|---|
  | Hex | `HexTag` | PK — one entity per coordinate |
  | HexResource | `HexResourcesComponent` | FK — N per coordinate (one per `ResourceType`) |
  | ResourceView | `ForestViewComponent` / `FishViewComponent` | FK — N per coordinate |
  | HexIconContainer | `HexIconContainerComponent` | FK — one per coordinate |

- One query definition has three materializations — pick by access pattern:

  ```csharp
  // PK table → unique index. TryGetEntity(key, out Entity).
  EntityMap<HexIdComponent> hexByCoord =
      world.GetEntities().With<HexTag>().AsMap<HexIdComponent>();

  // FK 1:N table → non-unique index. TryGetEntities(key, out ReadOnlySpan<Entity>).
  EntityMultiMap<HexIdComponent> resourcesByCoord =
      world.GetEntities().With<HexResourcesComponent>().AsMultiMap<HexIdComponent>();

  // The same table as a sweep set.
  EntitySet resources =
      world.GetEntities().With<HexIdComponent>().With<HexResourcesComponent>().AsSet();
  ```

- `EntityMap` / `EntityMultiMap` are self-maintaining: they update on `Set` / `Remove`. This works
  ONLY because of the "always write through `entity.Set<T>(value)`" rule above — a single
  ref-mutation silently desyncs every maintained index.
- Query caches held as system fields (`EntitySet`, `EntityMap`, `EntityMultiMap`) are declarative.
  They do NOT count as forbidden system state under the stateless-systems ban.
- The map API is Try-pattern (`bool` return + `out` result) — the same contract as the Collector
  convention above. Branch on the `bool`.
- **Key equality:** a key component MUST implement `IEquatable<T>` + `GetHashCode`, or an
  `IEqualityComparer<T>` MUST be passed to the `AsMap` / `AsMultiMap` overload.
  `HexIdComponent` (delegates to `HexCoord`) and `HexResourcesComponent` (keys on its `Type` enum)
  implement this — follow their shape for new key components.
- **Join = a lookup by key value at the point of use.** Never store an `Entity` reference from one
  table's row to another table's row.
- Maintained indexes are for hot joins (read every frame or many times per turn). A
  click-frequency query may linearly scan an `EntitySet` instead — do not build a map for it.
- Legacy bare-key queries exist and are flagged `⚠ BARE-KEY LEGACY` in `ECS_REFERENCE.md`
  (pending audit). Do NOT copy that pattern into new code.

### Link Convention — Domain ID vs Entity Handle
- A **domain / persistent relationship** is expressed as a stable domain ID component
  (e.g. `HexIdComponent`), never as a stored `Entity` handle.
- A **runtime-only link** — non-serialized, lifetime-coupled, typically view-layer (a view
  component holding its `MonoBehaviour`) — may hold a direct reference or an `Entity` handle.
- Rationale: a stable ID survives save/load and map regeneration; a stale ID fails loud at
  resolution (`Try…` + throw), a stale `Entity` handle fails silent.

### Error Handling — Fail Loud
- A step that cannot do its job correctly MUST throw, not silently succeed. Missing or invalid
  prerequisites (absent / `!Exist` config, null prefab, missing required component) are errors —
  throw a descriptive exception naming exactly what is missing.
- **Forbidden:** silent `return` / `return UniTask.CompletedTask` on a missing prerequisite, and
  `Debug.LogWarning(...)` + skip. A "completed" step that did nothing hides the bug — the goal is a
  working game or a hard, visible failure, not a pile of warnings.
- Validate at the source (the config loader throws on `!Exist`) AND keep a defensive throw at the
  consumer as a second barrier — do not assume validation happened elsewhere.
- `cancellationToken.IsCancellationRequested` is the ONE legitimate quiet `return`. Keep it on its own
  line, separate from error conditions (never `if (cancelled || !valid) return;`).
- Clean up partial work before throwing (e.g. `Object.Destroy(instance)`).
- Exception type is not critical; `InvalidOperationException` with a message is the codebase default
  (see `TerrainGenerationConfigLoaderSystem`).

## Boot And System Flow

- Boot phase markers live in `Boot.Core`: `ConfigLoadStep` (the one-time config bootstrap driven by
  `Boot` at startup) and `MapGenerationStep` (the world-init pipeline; renamed from the
  feature-specific `TerrainGenerationStep` — it is the shared world-init step, not terrain-only).
  `FirstUIStep` survives only as a marker argument — it is not a boot phase.
- After the bootstrap, `Boot` hands off to the hand-wired `GameModeMachine`
  (states: `MainMenu`, `MapCreation`, `MapLoading`, `Gameplay`). **Only the active state's systems
  run.** Each state lists its Update / LateUpdate systems explicitly in `Boot.Construct` —
  composition is manual and visible in one place. See `Assets/Modules/Boot/BOOT.md`.
- The world-init pipeline (`IPrioritizedUniTaskSystem<MapGenerationStep>`) is run by the
  `MapCreation` state: stages execute sequentially in ascending priority (map gen → resources →
  terrain view → resource views → selection view → debug → icon containers → info panel).
- Within a state, per-frame systems tick in ascending `Priority`; `EventCleanupSystem`
  (`int.MaxValue`) always runs last and disposes the frame's event entities.
- System base choice: config init → `ConfigLoaderSystem`; per-frame → `UpdatedSystem`;
  late-frame → `LateUpdatedSystem`; ordered async pipelines → `IPrioritizedUniTaskSystem<T>`
  (or `IUniTaskSystem<T>` + `UniTaskSequentialSystem<T>` when DI order suffices).
  See System Taxonomy above and `SYSTEMTEMPLATE.md`.

## On-Demand References
- For UI/UX visual style, component patterns, placement, and USS token mapping, read `GENERAL_UI_STYLE.md` (root) — read it **fully only when working on the UI / design part**
- For how to write any `.md` file in this project, read `DOC_STANDARD.md` (root)
- For ECS entity archetypes, world components, and event flows, read `ECS_REFERENCE.md` (root)
- For `IAddressable`, `Box<T>`, `Result<T>`, or addressable ownership rules, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`
- For terrain transition work, pre-read:
  - `Assets/Presentation/Terrain/Isolines/FieldBasedIsolineBuilder.cs`
  - `Assets/Presentation/Terrain/Isolines/IsolineSlopeTransition.cs`
  - `Assets/Presentation/Terrain/Smooth/HeightSmoothing.cs`
- For water-view-specific setup details, read `Assets/Presentation/Terrain/WATER_VIEW_SETUP.md`

## Module / Domain / Presentation Reference Files
Each module, domain, and presentation sub-area has an MD file in its folder. Read it before touching
any code there.

| Area | Reference File |
|---|---|
| `AxialSystem` | `Assets/Modules/AxialSystem/AXIAL_SYSTEM.md` |
| `Boot` | `Assets/Modules/Boot/BOOT.md` |
| `Cameras` | `Assets/Modules/Cameras/CAMERAS.md` |
| `Configs` | `Assets/Modules/Configs/CONFIGS.md` |
| `CurveBuilders` | `Assets/Modules/CurveBuilders/CURVE_BUILDERS.md` |
| `MainUI` | `Assets/Modules/MainUI/MAIN_UI.md` |
| `MainCanvas` | `Assets/Modules/MainCanvas/MAIN_CANVAS.md` |
| `UserInput` | `Assets/Modules/UserInput/USER_INPUT.md` |
| `Turn` | `Assets/Modules/Turn/TURN.md` |
| domain `Map` | `Assets/Domains/Map/MAP.md` |
| `Map/Hex` | `Assets/Domains/Map/Hex/HEX_CORE.md` |
| `Map/Generation` | `Assets/Domains/Map/Generation/TERRAIN_GENERATOR.md` |
| `Map/HexResources` | `Assets/Domains/Map/HexResources/HEXRESOURCES.md` |
| `Map/Pathfinding` | `Assets/Domains/Map/Pathfinding/PATHFINDING.md` |
| domain `Economy` | `Assets/Domains/Economy/ECONOMY.md` |
| domain `Actors` | `Assets/Domains/Actors/ACTORS.md` |
| `Presentation` | `Assets/Presentation/PRESENTATION.md` |
| `Presentation/Terrain` | `Assets/Presentation/Terrain/TERRAIN_VIEW.md` |
| `Presentation/HexResources` | `Assets/Presentation/HexResources/HEXRESOURCESVIEW.md` |
| `Presentation/HexIcons` | `Assets/Presentation/HexIcons/HEXICONS.md` |
