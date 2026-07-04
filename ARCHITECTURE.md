---
category: C
read: always
tags: [architecture, ecs, conventions]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ECS_CONVENTIONS](ECS_CONVENTIONS.md)"
---

# FantasyMayor — Architecture Reference

> Doc map & read-priority: see `INDEX.md` (generated; lists every doc and when to read it).
> ECS/runtime **conventions** (state storage, decomposition, the two hard bans, `Set()`-writes,
> collector Try-pattern, Table Rule, link convention, fail-loud) live in `ECS_CONVENTIONS.md` —
> read it before writing or editing any ECS system, component, event, config, or query.

## Stack
- Engine: Unity
- ECS: `DefaultEcs` with a DoD style, not Unity DOTS
- DI: `VContainer`
- Async: `UniTask`
- Asset loading: Unity `Addressables`
- Input: Unity `InputSystem`
- Rendering: Universal Render Pipeline
- UI: Unity `UI Toolkit` (UXML/USS); `Unity App UI` (`com.unity.dt.app-ui`) is the component foundation — see `GENERAL_UI_STYLE.md` §15

## Source Of Truth
| What | Where |
|---|---|
| Feature and domain runtime code | `Assets/Modules/*`, `Assets/Domains/*`, `Assets/Presentation/*` |
| Shared utility primitives (`Box<T>`, `Result<T>`, `FrameBox<T>`, `StateAllowedAttribute`) | `Assets/Scripts/Core` |
| Shared ECS base systems and loop contracts | `Assets/Scripts/DefaultECSExtensions` |
| App-root DI composition | `Assets/Scripts/Installers/*` |
| Authored config assets | `Assets/Addressables/Configs/*` |
| Scenes | `Assets/Scenes/*` |
| Package dependencies | `Packages/manifest.json` |

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
| `MainCanvas` | `MainCanvas.Core`, `MainCanvas.Implementation` | Root canvas abstraction and provider implementation; DI registration lives in `WorldInstaller` |
| `UserInput` | `UserInput` | Camera and player input ECS bridge plus camera movement config flow |
| `Turn` | `Turn` | Turn-phase orchestration engine: on a turn pulse runs ordered phase subsystems off the main thread and signals "turn in progress" via a world component. SCAFFOLD — no phases yet |

## Current Domains (`Assets/Domains/<Name>/`, one asmdef each)

Game-rule bounded contexts — pure data + logic, with **no view/render dependency**.

| Domain | Assembly | Current responsibility |
|---|---|---|
| `Map` | `Domains.Map` | The world-map bounded context: hex grid + terrain types (`Hex/`), procedural map generation (`Generation/` — `GenerationSystem` + Mountain/River/Lake/Sea subsystems, on `MapGenerationStep`), natural per-hex resources (`HexResources/`: Forest/Clay/Fish), hex pathfinding (`Pathfinding/`) |
| `Economy` | `Domains.Economy` | Owner-agnostic economic substrate: inventory resource types + the generic `ResourceLoadoutSpawner` mechanism, district scaffold; reads `Domains.Map` (hex types). No actor dependency |
| `Actors` | `Domains.Actors` | Actor identities (City, Mayor) + startup composition: per-actor spawn, Mayor config/loader, resource loadout; reads `Domains.Economy` |
| `Actions` | `Domains.Actions` | Application/orchestration layer: actor verbs + cross-domain turn processing; reads `Domains.Economy` + `Domains.Actors`. SCAFFOLD — no systems yet |

## Presentation Layer (`Assets/Presentation/`)

The render/view **tier**. Two dev-assemblies, both one-way onto the domains they render; **domains never
depend on either**:
- **`Presentation`** — the world/scene view (terrain, hex resources, hex icons), `Assets/Presentation/`
  root. Depends one-way on `Domains.Map` (plus `Cameras`, `CurveBuilders`).
- **`Presentation.UI`** — the screen-space HUD (UI Toolkit + App UI), `Assets/Presentation/UI/`. The former
  `MainUI` module, relocated into this tier. Depends on `Presentation` (shares icon configs) plus
  `Domains.Map` / `Domains.Economy` / `Domains.Actors` / `Turn`. Namespaces `Presentation.UI.*`.

| Sub-area | Assembly | Former module | Responsibility |
|---|---|---|---|
| `Terrain/` | `Presentation` | `Terrain.View` | Terrain mesh, textures, water view, isolines, smoothing, runtime view systems |
| `HexResources/` | `Presentation` | `HexResourcesView` | Resource visualization (forest/clay/fish views, ground painting) |
| `HexIcons/` | `Presentation` | `HexIcons` | Screen-space per-hex icon overlay |
| `UI/` | `Presentation.UI` | `MainUI` | Screen-space HUD (per-window subfolders): generator menu (MainMenu state), selection-driven hex info panel (Gameplay), End Turn button + context tabs, left-edge resource bar |

## Feature Placement & Layout Rules

Placement is **by layer** (see "Domains, Presentation & Modules" below for the layer definitions):

- New **game-rule (domain)** code goes to `Assets/Domains/<Domain>/<Feature>/` — into the bounded
  context that owns the rule (create a new domain only as a deliberate architecture decision).
- New **view / render / HUD** code goes to `Assets/Presentation/<Sub-area>/` (world views) or
  `Assets/Presentation/UI/<Window>/` (screen-space HUD).
- New **engine-facing infrastructure** (input, loading, canvas, boot, shared kernels) goes to
  `Assets/Modules/<FeatureName>/`.
- Feature-specific DI goes to the feature's own `Installer/` folder; cross-cutting or app-root DI
  goes to `Assets/Scripts/Installers/`.
- UI assets (prefabs, uxml, uss) go to the feature's `Prefabs/` folder.

Every feature folder — in whichever layer it lives — uses this target layout:

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

## DI Composition Rules

- `WorldInstaller` is the only `LifetimeScope` in the project.
- App-root composition happens in `WorldInstaller.Configure()`.
- Module installer classes should implement `VContainer.IInstaller`.
- Use a plain class for an installer when it has no serialized data.
- Use `MonoBehaviour + IInstaller` only when an installer must own `[SerializeField]` data.
- Installer execution order is explicit in `WorldInstaller.Configure()`; dependencies must be installed before dependents.
- Per-frame systems are registered with their **concrete** type (`.As<TheSystem>()`), not as
  `IUpdatedSystem`/`ILateUpdatedSystem` — `Boot` injects concretes and wires them into game states by hand.

## Layout Deviations
- The repository is partially standardized, not fully uniform. Per-module deviation notes live in each
  module's own MD (INDEX "Reference map"), not here.
- `Assets/Scripts/Extentions` is a legacy typo-named folder — existing structure, not a naming standard.

## Domains, Presentation & Modules (architecture)
Three top-level code layers, boundary enforced by asmdef references:

- **Domain** (`Assets/Domains/<Name>/`) = a game-rule **bounded context** (DDD-strategic): owns its
  entity tables and turn-phase logic. Pure data + logic, **free of any view/render dependency**.
  Current: `Map`, `Economy`, `Actors`, `Actions`. They form the DAG **substrate → agents → verbs**:
  `Map`/`Economy` (leaves) → `Actors` (agents) → `Actions` (verbs).
- **Presentation** (`Assets/Presentation/`) = the **render/view tier**: world/scene views (`Presentation`
  assembly: terrain/resource/icon) + the screen-space HUD (`Presentation.UI` assembly: the former `MainUI`,
  under `UI/`). Each depends one-way on the domains it renders; **domains never depend on it**.
- **Module** (`Assets/Modules/`) = engine-facing infrastructure plumbing: addressables, input,
  cameras, canvas, boot, config providers, shared kernels (`AxialSystem`, `CurveBuilders`).

**Why this shape.** This is **DDD-strategic bounded contexts + a layered presentation tier**, on top of a
**DoD/ECS** data substrate. DDD (Evans) deliberately isolates the domain model from UI/infra, so pulling
all views into one layer is *pro*-DDD, not vertical-slice. DoD's "no hierarchy" is about data/types
(flat tables, composition, no inheritance — see the Table Rule, `ECS_CONVENTIONS.md`); it is orthogonal
to this code-layering. We borrow DDD's **strategic** half (contexts, ubiquitous language, layering), not
its OO **tactical** patterns (aggregates/repositories), which ECS expresses as tables + systems.

- One asmdef per domain; the presentation tier has two (`Presentation` for world views, `Presentation.UI`
  for the HUD) — feature subfolders inside; namespaces follow: `Domains.Map.Hex.*`, `Presentation.Terrain.*`,
  `Presentation.UI.ResourceBar.*`, …).
- Cross-domain dependencies are expected (e.g. `Economy → Map` for hex types, `Actors → Economy` for the
  resource substrate, `Actions → {Economy, Actors}`) and are visible in the ECS/DoD graph (`/ecs-graph`,
  cross-module component reads). Direction follows the substrate→agents→verbs DAG — owner-keyed logic
  lives in `Actors`/`Actions`, never in the owner-agnostic `Economy` substrate.
- The Feature Placement & Layout Rules above apply to all three layers (each feature/sub-area keeps the
  `Components`/`Systems`/… split).

## Public Contract Split
- When a module exposes a reusable public API, prefer a `Core/` contract assembly plus an `Implementation/` assembly.
- Current canonical examples are `Addressable`, `MainCanvas`, and `Boot`.

---

## System Taxonomy

Canonical vocabulary. Every system in the project plays exactly ONE of these roles. Use these names
in docs, reviews, and design discussions. The concrete skeleton for each role lives in `Patterns/` — see
**Pattern Recipes** below.

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
sorting, subsystem query ownership, pulse + reconcile shape) → the matching `Patterns/PATTERN_*.md`
recipe (index in **Pattern Recipes** below). This file owns the *taxonomy*; `ECS_CONVENTIONS.md` owns
the runtime conventions; the recipes own the *procedure*.

Role invariants (policy — hold regardless of the template you follow):
- One-frame events DO NOT survive the async `MapCreation` pipeline (`EventCleanupSystem` disposes them
  at end of tick) — startup bulk work is always a Pipeline Stage/SubSystem, never event-driven.
- A **Reactive System** is the DEFAULT for runtime logic. A **Per-frame System** must justify in
  writing why it cannot be reactive; no justification = a decomposition smell
  (see `ECS_CONVENTIONS.md` → Decomposition Rules).
- An **Orchestrator** contains no domain logic of its own; a **SubSystem** exists only under an
  orchestrator.
- Naming: orchestrators and stages are named `…System`, subsystems `…SubSystem`. Reactive systems carry
  intent names (`ForestSpawnSystem`, `HexIconsVisibilitySystem`) — there is no mandated
  `…ReactiveSystem` suffix.
- Type names never repeat their owning **domain** — the namespace carries it (`GenerationSystem`, not
  `MapGenerationSystem`). Full rule + exceptions (FK/PK identity components, DI installers):
  `ECS_CONVENTIONS.md` → Naming & Construction.

**Turn pipeline (module `Turn`) — same roles, different scope.** The Orchestrator/SubSystem roles are
reused for turn processing, but turn-scoped (re-run every turn on a `NextTurnEvent` pulse) and executed
OFF the main thread (`UniTask.RunOnThreadPool`), unlike the one-shot, main-thread `MapCreation`
pipeline. The phase base is `TurnPhaseSubSystem`; the launcher is the per-frame `TurnProcessorSystem`
(it polls the in-flight run each frame, so it is justified as a Per-frame System, not reactive). Every
world write stays on the main thread; the pool only computes. SCAFFOLD — zero phases today.

## Pattern Recipes

Minimal, **one-approach-per-file** skeletons for the building blocks below — read the matching recipe
instead of copying a live implementation. Each is a Category B doc in `Patterns/` (also in `INDEX.md`).
These replace the retired `SYSTEMTEMPLATE.md` / `CONFIGTEMPLATE.md` monoliths; this file owns the
*taxonomy*, `ECS_CONVENTIONS.md` owns the *conventions*, the recipes own the *procedure*.

| Read when you are creating… | Recipe |
|---|---|
| an ECS data component (struct of runtime values; a key component needs `IEquatable`) | `Patterns/PATTERN_COMPONENT.md` |
| a field-less marker / table discriminator (empty struct) | `Patterns/PATTERN_TAG.md` |
| a one-frame event (payload-less pulse + `EventTag`) | `Patterns/PATTERN_EVENT.md` |
| a `ScriptableObject` config + its runtime component (flatten vs wrap-SO) | `Patterns/PATTERN_CONFIG.md` |
| a config loader (`ConfigLoadStep`, `Box`, validate, `world.Set`) | `Patterns/PATTERN_CONFIG_LOADER.md` |
| a world-init pipeline stage (spawn / build once during map creation) | `Patterns/PATTERN_PIPELINE_STAGE.md` |
| an orchestrator + DI-collected subsystem family (DoD polymorphism) | `Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md` |
| a polymorphic SO config catalogue materialized into an entity table (many kinds keyed by a shared FK; + optional per-kind evaluator) | `Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md` |
| a per-frame system (continuous logic; `PreUpdate` + `FrameBox`) | `Patterns/PATTERN_PERFRAME_SYSTEM.md` |
| a reactive system (event-driven — the DEFAULT for runtime logic) | `Patterns/PATTERN_REACTIVE_SYSTEM.md` |
| one-frame event cleanup (and why you almost never write one) | `Patterns/PATTERN_CLEANUP_SYSTEM.md` |

## ECS & Runtime Conventions → `ECS_CONVENTIONS.md`

All point-of-code rules moved to `ECS_CONVENTIONS.md` (`read: trigger` — before writing or editing any
ECS system, component, event, config, or query). It owns: **State Storage Taxonomy** (entity table /
world component / one-frame event / singleton entity), **Decomposition Rules** (god-system smells,
pulse + reconcile), **Naming & Construction** (suffix rule, chaining, instance-by-default), the two
hard bans (**stateless systems**, **no `System.Collections.Generic` in systems**), **Component Writes
And Reactivity** (`Set()`-only writes), **Collector And Output Methods** (Try-pattern), **Relational
Modeling — Table Rule** (PK/FK, `AsMap`/`AsMultiMap`), **Link Convention** (domain ID vs `Entity`
handle), and **Error Handling — Fail Loud**.

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
  See System Taxonomy above and the `Patterns/` recipes (see **Pattern Recipes**).

## On-Demand References
- For ECS/runtime conventions (state storage, decomposition, bans, writes, Table Rule, fail-loud),
  read `ECS_CONVENTIONS.md` — required before any ECS code work
- For UI/UX visual style, component patterns, placement, and USS token mapping, read `GENERAL_UI_STYLE.md` (root) — read it **fully only when working on the UI / design part**
- For how to write any `.md` file in this project, read `DOC_STANDARD.md` (root)
- For the concrete skeleton of any system / config / component / tag / event, read the matching
  `Patterns/PATTERN_*.md` (index in **Pattern Recipes**)
- For ECS entity archetypes, world components, and event flows, query the ECS/DoD graph (`/ecs-graph`)
- For `IAddressable`, `Box<T>`, `Result<T>`, or addressable ownership rules, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`
- For terrain transition work, pre-read:
  - `Assets/Presentation/Terrain/Isolines/FieldBasedIsolineBuilder.cs`
  - `Assets/Presentation/Terrain/Isolines/IsolineSlopeTransition.cs`
  - `Assets/Presentation/Terrain/Smooth/HeightSmoothing.cs`
- For water-view-specific setup details, read `Assets/Presentation/Terrain/WATER_VIEW_SETUP.md`

## Module / Domain / Presentation Reference Files
Each module, domain, and presentation sub-area has an MD file in its folder — **read it before
touching any code there**. The full generated list (with status + one-liners) is `INDEX.md` →
"Reference map"; do not duplicate it here.
