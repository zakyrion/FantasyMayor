---
category: A
read: reference
tags: [boot, ecs, state-machine]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
status: partial
---

# Boot

Entry-point orchestration: a one-time config bootstrap, then a hand-wired game-state machine.

## Shape

`Boot` (MonoBehaviour) does two things:
1. **Bootstrap** — on `Start`, runs all `IUniTaskSystem<ConfigLoadStep>` sequentially (configs, singletons).
   This is one-time and outside the state machine.
2. **Drive the machine** — builds a `GameModeMachine` in `Construct` and ticks it every `Update` /
   `LateUpdate`.

`ConfigLoadStep` is the only remaining boot *phase*. `FirstUIStep` is **no longer a boot phase** — the
menu UI is now owned by the `MainMenu` state. (`FirstUIStep` survives only as the marker argument of
`ShowHexesUISystem.Update`.)

## Game-State Machine (`GameModeMachine`)

Four modes (`GameMode`): `MainMenu`, `MapCreation`, `MapLoading`, `Gameplay`. Only one is active at a time;
**only the active state's systems run** (exclusivity is the whole point — no flat always-on list anymore).

Each state implements `IAppState`:
- `EnterAsync(ct)` — async entry; ticking is suspended until it completes.
- `Tick(GameState)` / `LateTick(GameState)` — per-frame while active.
- `Exit()` — cleanup on leave.
- `RequestedMode` — non-null asks the machine to switch; read after each `Tick`. Reset in `EnterAsync`.

Current flow:
```
Boot.Start: await ConfigLoad → machine.Switch(MainMenu)
MainMenu    : Enter shows generator UI; on TerrainGenerationGenerateEventComponent → Exit hides UI → MapCreation
MapCreation : EnterAsync runs the generation pipeline, then ticks its systems for N "settle" frames → Gameplay
Gameplay    : steady per-frame tick; no auto-transition
MapLoading  : stub (no save/load flow yet)
```

### Why a "settle" frame pump in MapCreation
The generation pipeline (`IPrioritizedUniTaskSystem<TerrainGenerationStep>`) builds both logical data and
the views synchronously (forest is now built one-shot by `ForestResourceViewSubSystem` inside the pipeline,
not reactively over later frames). `MapCreation` still ticks its settle-frame systems (`EventCleanupSystem`)
for a small fixed number of frames (`SettleFrames`, 1–3) to drain anything the pipeline raised before
handing off to `Gameplay`. Safe — those systems are idempotent, so extra ticks are no-ops.

## Manual Wiring (deliberate)

`Boot.Construct` injects every per-frame system as a **concrete singleton** and composes the states by hand
— Boot explicitly knows which system belongs to which mode. This is a conscious move away from
auto-collected `IReadOnlyList<IUpdatedSystem>` toward an explicit composition root. Consequence:
`Boot.Implementation` references the module assemblies whose systems it wires (`Hexes.UI`, `UserInput`,
`Terrain.View`, `HexResourcesView`, `HexIcons`).

Module installers register these systems with their **concrete** type (`.As<TheSystem>()`), not as
`IUpdatedSystem`/`ILateUpdatedSystem`. Systems stay DI-constructed singletons; only their grouping is manual.

A system may belong to several states — it is simply referenced from each. Current overlaps:
- `EventCleanupSystem` → `MapCreation` and `Gameplay`.

(Forest no longer overlaps states: the startup build is a one-shot pipeline subsystem
`ForestResourceViewSubSystem`, and `ForestSpawnSystem`/`ForestDespawnSystem` are `Gameplay`-only.)

`EventCleanupSystem` lives in `DefaultECS.Extensions` (not the installer assembly) so Boot can wire it
without an assembly cycle (`Installers.World` already references `Boot.Implementation`).

### Starting per-state composition
| State | Update systems | LateUpdate systems |
|---|---|---|
| MainMenu | — (waits for the Generate event) | — |
| MapCreation | generation pipeline (async entry) + EventCleanup (settle frames) | — |
| Gameplay | HexSelection, HexSelectionView, ForestSpawn, ForestDespawn, HexIconsVisibility, HexInfoPanel, HexInfoPanelHeader, HexInfoPanelResources, HexInfoPanelDistrictPlaceholder, EventCleanup | CameraMovement, HexIconsContainerPosition |
| MapLoading | — (stub) | — |

Role mix (per `ARCHITECTURE.md` "System Taxonomy"): `MapCreation` drives one-shot **Pipeline
Stages** (100–800) through its async entry; `Gameplay` ticks **Per-frame Systems** (HexSelection,
HexSelectionView, HexInfoPanel, CameraMovement, HexIconsContainerPosition) and **Reactive Systems**
(ForestSpawn/Despawn, HexIconsVisibility, the three HexInfoPanel block systems) plus the Cleanup.

## Non-Obvious Invariants
- Boot phase markers are empty structs used only as generic type tags. `ConfigLoadStep` is driven by Boot;
  `TerrainGenerationStep` is driven by the `MapCreation` state (was `WorldInitSystem`, now removed).
- `Update` / `LateUpdate` do nothing until the config bootstrap finishes.
- Within a state, systems are ticked in ascending `Priority` (e.g. `EventCleanupSystem` = `int.MaxValue`
  runs last, clearing one-frame event entities).
- `GameModeMachine` cancels an in-flight `EnterAsync` if a new switch happens or on dispose.
- Re-entry / return-to-menu with world teardown is **not** implemented yet — the flow is currently linear
  (`MainMenu → MapCreation → Gameplay`).

## Current State
State-machine skeleton in place. MainMenu, MapCreation, Gameplay implemented; MapLoading is a stub.
