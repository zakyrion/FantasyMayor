# FantasyMayor — Architecture Reference

## Stack
- Engine: Unity
- ECS: `DefaultEcs` with a DoD style, not Unity DOTS
- DI: `VContainer`
- Async: `UniTask`
- Asset loading: Unity `Addressables`
- Input: Unity `InputSystem`
- Rendering: Universal Render Pipeline

## Repository Map

```text
FantasyMayor/
├─ Assets/
│  ├─ Modules/                # Domain and feature modules
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
├─ CLAUDE.md
├─ SYSTEMTEMPLATE.md
└─ CONFIGTEMPLATE.md
```

## Source Of Truth
| What | Where |
|---|---|
| Feature and domain runtime code | `Assets/Modules/*` |
| Shared runtime primitives | `Assets/Scripts/Core` |
| Shared ECS base systems | `Assets/Scripts/DefaultECSExtensions` |
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
| `Core` | `Assets/Scripts/Core` | Shared utility primitives such as `Box<T>`, `Result<T>`, disposal helpers, status primitives |
| `DefaultECS.Extensions` | `Assets/Scripts/DefaultECSExtensions` | ECS base system types such as `UpdatedSystem`, `LateUpdatedSystem`, `ConfigLoaderSystem`, `IUniTaskSystem<T>` |
| `Installers` | `Assets/Scripts/Installers/Addressable` | App-root `IInstaller` implementations such as `AddressableInstaller` |
| `Installers.World` | `Assets/Scripts/Installers/World` | Root `LifetimeScope`, world composition, input wiring, event cleanup, installer orchestration |

## Current Modules

| Module | Assemblies | Current responsibility |
|---|---|---|
| `Addressable` | `Addressables.Core`, `Addressables.Implementations` | Public addressable loading contract and implementation |
| `AxialSystem` | `AxialSystem` | Axial hex math, coordinates, generic axial grid primitives |
| `Boot` | `Boot.Core`, `Boot.Implementation` | Boot pipeline step markers and boot MonoBehaviour orchestration |
| `Cameras` | `Cameras` | Owns the active scene camera as a single-instance world component (`CameraComponent`), decoupled from consumers |
| `Configs` | no dedicated asmdef | Generic config provider abstractions used by runtime code |
| `CurveBuilders` | `CurveBuilders` | Shared curve builder contract |
| `HexCore` | `Hex.Core` | Hex domain data, tags, grid and utility operations |
| `HexesUI` | `Hexes.UI` | UI Toolkit based controls and first-step UI flow |
| `MainCanvas` | `MainCanvas.Core`, `MainCanvas.Implementation` | Root canvas abstraction and provider implementation; DI registration lives in `WorldInstaller` |
| `Pathfinding` | `Pathfinding`, `Pathfinding.Installer` | Hex pathfinding utility and its DI registration |
| `TerrainGenerator` | `Terrain.Generator` | Terrain generation configs, event trigger, generation systems for mountains, rivers, lakes, sea |
| `TerrainView` | `Terrain.View` | Terrain mesh, textures, water view, isolines, smoothing, runtime view systems |
| `UserInput` | `UserInput` | Camera and player input ECS bridge plus camera movement config flow |
| `HexResources` | `HexResources` | Resource tag components, generation systems, config flow for Forest, Clay, Fish |
| `HexResourcesView` | `HexResourcesView` | Resource visualization. Forest is a **reactive** per-frame `UpdatedSystem` (`ForestViewSyncSystem`: trees + green ground painted into the persistent terrain texture); Clay/Fish are one-shot pipeline scaffolds |

## Module Layout Rules

Every new feature should still prefer `Assets/Modules/<FeatureName>/` and use this target layout:

```text
<FeatureName>/
├─ Components/   # Pure data structs
├─ Tags/         # tags conponents for Entities
├─ Events/       # event conponents for systems
├─ Configs/      # ScriptableObject class definitions
├─ Data/         # Collections, records, enums, helper types
├─ Systems/      # ECS systems and runtime orchestration
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

Folder contract:
| Folder | Never contains |
|---|---|
| `Components/` | Logic or side effects |
| `Configs/` | Runtime logic or config assets |
| `Data/` | ECS systems or MonoBehaviours |
| `Systems/` | View logic or config definitions |
| `Views/` | Business logic |
| `Installer/` | Anything except DI registration |

## Current Structure Notes And Exceptions
- The repository is partially standardized, not fully uniform.
- `TerrainGenerator` and `TerrainView` are the closest matches to the full module template.
- `HexesUI` currently uses `Installer`, `Systems`, and `Prefabs`, but does not have the full standard folder set.
- `AxialSystem`, `CurveBuilders`, and `Configs` are flatter utility-style modules rather than full feature modules.
- `Boot`, `Addressable`, and `MainCanvas` use explicit contract and implementation splits.
- `Pathfinding` has a root runtime assembly plus a separate installer assembly that exposes an `IInstaller`.
- `UserInput` currently has `Components`, `Configs`, and `Systems`, but no module-local installer folder.
- `Assets/Scripts/Extentions` is a legacy typo-named folder and should be treated as existing structure, not a naming standard.

## Public Contract Split
- When a module exposes a reusable public API, prefer a `Core/` contract assembly plus an `Implementation/` assembly.
- Current canonical examples are `Addressable`, `MainCanvas`, and `Boot`.

## ECS And Runtime Conventions
- **Component naming by role (suffix):**
  - a component **carrying data** → `…Component` (e.g. `HexIdComponent`, `HexIconsVisibilityComponent`)
  - a **tag / marker** component (empty, presence-only) → `…Tag` (e.g. `HexTag`, `EventTag`)
  - a **one-frame event** component → `…Event` (e.g. `HexIconsVisibilityChangedEvent`)

  Pre-existing `…EventComponent` names (e.g. `TerrainGenerationGenerateEventComponent`) predate this rule;
  they stay until a deliberate rename, but new events use the `…Event` suffix.
- Single-component entity creation may chain: `world.CreateEntity().Set(...)`
- Once more than one component is assigned, stop chaining and use a local entity variable
- In `MonoBehaviour` code, prefer `System.Collections.Generic`
- In ECS systems, prefer `Unity.Collections` and dispose persistent allocations explicitly
  - **Exception — managed elements:** a collection whose elements are managed types (`GameObject`,
    `MonoBehaviour`/view references, `Entity`-wrapping records, etc.) stays `System.Collections.Generic`,
    because `NativeContainer` only holds `unmanaged` types. Mark such cases with a short comment.
  - Enums can't be `NativeHashSet`/`NativeParallelHashMap` **keys** (no `IEquatable<T>`) — key on the
    underlying `int`. As a **value** an enum is fine (`unmanaged`).
  - Use `Allocator.Temp` for within-frame scratch; use `Allocator.Persistent` (with explicit dispose)
    when a container must outlive an `await` / cross a `RunOnThreadPool` boundary.
- Prefer instance-based design; use `static` only when a type is truly stateless utility infrastructure

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
  uses `WhenAdded` / `WhenRemoved`, not `WhenChanged`. Migrating such a system is a deliberate per-case
  refactor. See `Assets/Modules/HexResourcesView/HEXRESOURCESVIEW.md`.

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
- Reference example: `ForestViewSyncSystem.TryPickForestEntry` — input as a parameter, result via `out`,
  `bool` return.
- A single-use private helper that reads class fields and just unfolds the caller's linear flow does **not**
  earn extraction — inline it into the caller. Extract only when the method is signature-complete (inputs
  as parameters) and is either reused or a self-contained operation. `Update` (a system's orchestration
  root) may read the system's own fields directly.

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

### Config Component Storage
- A loaded config's flattened ECS component is stored as a **world component** —
  `world.Set<TConfigComponent>(value)` — **not** on a created singleton entity. Read it with
  `world.Get<TConfigComponent>()`, guarded by `world.Has<TConfigComponent>()`.
- Rationale: a config is load-once, single-instance state. The world component is the one
  canonical slot for it — there is no singleton entity to locate, and no second copy can be
  created by accident.
- A world component is **not an entity**. It never appears in `world.GetEntities()` and cannot be
  matched by entity-query filters (`With<T>`, `WhenAdded<T>`, `WhenChanged<T>`) — those operate on
  entities only. Consumers read it directly via `world.Get<T>()`. If a config change must drive
  reactive consumers, publish an explicit event component on an entity; do not expect a world
  component to surface in an `EntitySet`.
- The same applies to other genuinely single-instance world state, not only configs.
- `CONFIGTEMPLATE.md` is the canonical template for the loader that performs this write.

## Boot And System Flow
- Boot step markers live in `Boot.Core` as `ConfigLoadStep`, `FirstUIStep`, and `TerrainGenerationStep`
- `ConfigLoadStep` is the one-time config bootstrap driven by `Boot` at startup. After it, `Boot` hands off to a hand-wired `GameModeMachine` (states: `MainMenu`, `MapCreation`, `MapLoading`, `Gameplay`); only the active state's systems run.
- `TerrainGenerationStep` is the generation pipeline (`IPrioritizedUniTaskSystem<TerrainGenerationStep>` stages: terrain gen → resources → terrain view → resource view → selection → debug). It is now run by the **`MapCreation` state** (the old `WorldInitSystem` was removed). `FirstUIStep` is no longer a boot phase — the generator UI belongs to the `MainMenu` state. Per-frame systems are registered as concrete singletons and wired into states by hand in `Boot`. See `Assets/Modules/Boot/BOOT.md`.
- Config initialization systems should derive from `ConfigLoaderSystem`
- Regular per-frame logic should derive from `UpdatedSystem`
- Late-frame logic should derive from `LateUpdatedSystem`
- Ordered async subsystem pipelines can use `IUniTaskSystem<T>` and `UniTaskSequentialSystem<T>`

## On-Demand References
- For how to write any `.md` file in this project, read `DOC_STANDARD.md` (root)
- For ECS entity archetypes (runtime component compositions) and component read/write maps, read `ECS_REFERENCE.md` (root)
- For `IAddressable`, `Box<T>`, `Result<T>`, or addressable ownership rules, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`
- For terrain transition work, pre-read:
  - `Assets/Modules/TerrainView/Isolines/FieldBasedIsolineBuilder.cs`
  - `Assets/Modules/TerrainView/Isolines/IsolineSlopeTransition.cs`
  - `Assets/Modules/TerrainView/Smooth/HeightSmoothing.cs`
- For water-view-specific setup details, read `Assets/Modules/TerrainView/WATER_VIEW_SETUP.md`

## Module Reference Files
Each module has an MD file in its root folder. Read it before touching any code in that module.

| Module | Reference File |
|---|---|
| `AxialSystem` | `Assets/Modules/AxialSystem/AXIAL_SYSTEM.md` |
| `Boot` | `Assets/Modules/Boot/BOOT.md` |
| `Cameras` | `Assets/Modules/Cameras/CAMERAS.md` |
| `Configs` | `Assets/Modules/Configs/CONFIGS.md` |
| `CurveBuilders` | `Assets/Modules/CurveBuilders/CURVE_BUILDERS.md` |
| `HexResources` | `Assets/Modules/HexResources/HEXRESOURCES.md` |
| `HexResourcesView` | `Assets/Modules/HexResourcesView/HEXRESOURCESVIEW.md` |
| `HexCore` | `Assets/Modules/HexCore/HEX_CORE.md` |
| `HexesUI` | `Assets/Modules/HexesUI/HEXES_UI.md` |
| `MainCanvas` | `Assets/Modules/MainCanvas/MAIN_CANVAS.md` |
| `Pathfinding` | `Assets/Modules/Pathfinding/PATHFINDING.md` |
| `TerrainGenerator` | `Assets/Modules/TerrainGenerator/TERRAIN_GENERATOR.md` |
| `TerrainView` | `Assets/Modules/TerrainView/TERRAIN_VIEW.md` |
| `UserInput` | `Assets/Modules/UserInput/USER_INPUT.md` |
