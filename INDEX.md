---
category: C
read: always
tags: [index, navigation]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# INDEX

> ⚠️ **Key file — the single entry point for all doc navigation. Keep it short and informative.** Built in 2 passes: (1) `python3 Tools/gen_index.py` rebuilds the skeleton between the markers from each doc's frontmatter + first line; (2) the agent curates descriptions, statuses, and context. To change a description or status, edit the doc's first line / `status` frontmatter and re-run pass 1 — do not edit between the markers. The agent zone below the END marker is preserved across runs.

<!-- BEGIN GENERATED — Tools/gen_index.py rebuilds everything between these markers; edits here are overwritten -->

Totals: 52 docs — 2 always · 17 trigger · 33 reference · 3 canvas.

## Read at start (always)

Read these every session before doing anything else.

- [FantasyMayor — Architecture Reference](ARCHITECTURE.md) — `Unity App UI` (`com.unity.dt.app-ui`) as the component foundation — see `GENERAL_UI_STYLE.md` §15
- [CLAUDE.md](CLAUDE.md) — This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Read on demand (by trigger)

Do **not** preload. Read only when the trigger condition holds.

| Doc | Read it… | What it is |
|---|---|---|
| [IAddressable Contract](Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md) | before writing/editing/reviewing Addressables, IAddressable, Box<T> or Result<T> code | Single source of truth for addressable loading. Read this; do not grep. |
| [DOC_STANDARD.md](DOC_STANDARD.md) | before authoring or reviewing any .md for DOC_STANDARD compliance (the docs-curator's charter; the main agent reads it only when it authors a doc itself) | Single source of truth for how to write Markdown docs in this project. |
| [FantasyMayor — ECS & Runtime Conventions](ECS_CONVENTIONS.md) | before writing or editing any ECS system, component, event, config, or query | The ECS/runtime rulebook: where state lives, how systems are decomposed, and the write / collection / |
| [FantasyMayor - Gameplay Foundation](GAMEPLAY_FOUNDATION.md) | ONLY when the user explicitly asks to open this file — never on session-start, never by topic/keyword | `FantasyMayor` is a turn-based game about governing a city through a scarcity of `Action Points`, limited resources, population as a productive and political force, and an unstable balance of power between the mayor and the local elites. |
| [GENERAL_UI_STYLE.md](GENERAL_UI_STYLE.md) | before creating or changing UI (UI Toolkit, panels, tokens, USS) | The general UI design language for FantasyMayor: the global HUD layout model, design principles, visual |
| [GLOSSARY — domain vocabulary → code anchors](GLOSSARY.md) | when a domain term (any language) needs its canonical code name before searching roslyn / ecs-graph / di-graph | Map from human vocabulary (game-design terms, Ukrainian/English synonyms, abbreviations) to the |
| [Pattern — One-Frame Event Cleanup](Patterns/PATTERN_CLEANUP_SYSTEM.md) | before writing any one-frame-event cleanup (and to learn why you usually should not) | **You almost never write a cleanup system.** There is ONE global `EventCleanupSystem` (DefaultECSExtensions): a |
| [Pattern — ECS Data Component](Patterns/PATTERN_COMPONENT.md) | before creating an ECS data component (a struct holding runtime values) | A component is a plain `struct` of runtime values. No behavior, no methods (except equality when it is a |
| [Pattern — Config (ScriptableObject + Component)](Patterns/PATTERN_CONFIG.md) | before creating a ScriptableObject config and its runtime component | Authored data lives in a `ScriptableObject`, loaded via Addressables, and published as a **world component** |
| [Pattern — Config Loader System](Patterns/PATTERN_CONFIG_LOADER.md) | before creating a config loader system | A one-shot system that runs at `ConfigLoadStep`: loads config SO(s) from Addressables, validates them, and |
| [Pattern — One-Frame Event (Pulse)](Patterns/PATTERN_EVENT.md) | before creating a one-frame ECS event (pulse) | An event is a **payload-less `struct`** raised on its own entity for exactly one frame. It says "something |
| [Pattern — Orchestrator + SubSystems](Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md) | before creating an orchestrator + subsystem family (DoD polymorphism / independently ordered parts) | A family of implementations behind one abstract base, DI-collected into an orchestrator that sequences them by |
| [Pattern — Per-Frame System](Patterns/PATTERN_PERFRAME_SYSTEM.md) | before creating a per-frame system (genuinely continuous logic) | Logic that is genuinely continuous: camera movement, per-frame projection, input polling, selection watching. |
| [Pattern — Pipeline Stage (one-shot, world-init)](Patterns/PATTERN_PIPELINE_STAGE.md) | before creating a world-init pipeline stage (build/spawn content once during map creation) | One-shot async construction during map creation: spawn entities/views, build runtime world components, load |
| [Pattern — Polymorphic Config Catalogue → Entity Table](Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md) | before creating a polymorphic ScriptableObject config catalogue that materializes into an entity table (many kinds keyed by a shared FK), or a per-kind polymorphic system family over such a table | A **heterogeneous** set of authored rules/effects — many *kinds*, each with its own parameters — that you (1) author as |
| [Pattern — Reactive System (pulse + reconcile)](Patterns/PATTERN_REACTIVE_SYSTEM.md) | before creating a reactive (event-driven) system | **The default for runtime logic.** Responds to a one-frame [event](PATTERN_EVENT.md): the event is the base |
| [Pattern — ECS Tag](Patterns/PATTERN_TAG.md) | before creating an ECS tag (field-less marker / table discriminator) | A tag is an **empty `struct`** that marks an entity. It carries no data; its presence IS the information. |

## Reference map (on demand)

Per-module navigation docs. `status` mirrors each module's `## Current State`.

| Doc | Cat | Status | What it is |
|---|---|---|---|
| [Actions](Assets/Domains/Actions/ACTIONS.md) | A | partial | The application / orchestration layer: actor verbs and cross-domain turn processing. Depends on both |
| [Build District Action](Assets/Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md) | A | scaffold | The owner-scoped **build verb** — eventually turns an owner's confirmed choice into a live, multi-turn |
| [Turn Phases](Assets/Domains/Actions/TURN_PHASES.md) | A | partial | The Actions domain's turn-phase subsystems: phase **content** that plugs into the `Turn` engine. |
| [Actors](Assets/Domains/Actors/ACTORS.md) | A | partial | Game-rule domain owning actor identities **and their startup composition**. First domain under |
| [District Build Cost](Assets/Domains/Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md) | A | partial | The district-build **cost** catalogue: the per-district AP + resource price for building a district type. |
| [District Build Outcome](Assets/Domains/Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md) | A | partial | The district-build **outcome** catalogue: the per-district consequence that runs when a district finishes |
| [District Open Conditions](Assets/Domains/Economy/DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md) | A | partial | The district-build **unlock** rules ("how to unblock building of a district type"), authored as a polymorphic |
| [Economy](Assets/Domains/Economy/ECONOMY.md) | A | partial | Game-rule domain owning economic objects: inventory resources now; districts and buildings later. |
| [Kernel](Assets/Domains/Kernel/KERNEL.md) | A | implemented | The DDD Shared Kernel: cross-context vocabulary tokens that would otherwise force a dependency |
| [TerrainGenerator](Assets/Domains/Map/Generation/TERRAIN_GENERATOR.md) | A | implemented | Procedural terrain generation: hex grid creation, mountains with foothills, and water (river / lake / sea). |
| [HexCore](Assets/Domains/Map/Hex/HEX_CORE.md) | A | implemented | Core hex grid data structures and the per-hex terrain type. |
| [HexResources](Assets/Domains/Map/HexResources/HEXRESOURCES.md) | A | implemented | Generates logical resource data for the map. Does not render anything. |
| [Map](Assets/Domains/Map/MAP.md) | A | implemented | The world-map rule domain: the hex grid + terrain types, procedural generation, per-hex resources, and pathfinding. |
| [Pathfinding](Assets/Domains/Map/Pathfinding/PATHFINDING.md) | A | implemented | Hex-grid BFS pathfinding over ECS entities using native Unity collections. |
| [AxialSystem](Assets/Modules/AxialSystem/AXIAL_SYSTEM.md) | A | implemented | Hex grid coordinate system: axial math, coordinate types, and generic sparse grid storage. |
| [Boot](Assets/Modules/Boot/BOOT.md) | A | partial | Entry-point orchestration: a one-time config bootstrap, then a hand-wired game-state machine. |
| [Cameras](Assets/Modules/Cameras/CAMERAS.md) | A | implemented | Owns the shared scene-camera reference as world state, decoupled from any consumer module. |
| [Configs](Assets/Modules/Configs/CONFIGS.md) | A | implemented | Generic async loader pattern for ScriptableObject configs from Addressables. |
| [CurveBuilders](Assets/Modules/CurveBuilders/CURVE_BUILDERS.md) | A | implemented | Interface contract for building and evaluating animation curves used in terrain generation. |
| [MainCanvas](Assets/Modules/MainCanvas/MAIN_CANVAS.md) | A | implemented | Singleton provider for the main UI canvas root, behind an interface for DI. |
| [Turn](Assets/Modules/Turn/TURN.md) | A | partial | Engine that runs a game turn: on a pulse it fires ordered phase subsystems off-thread, gating other systems. |
| [UserInput](Assets/Modules/UserInput/USER_INPUT.md) | A | implemented | Bridges Unity InputSystem to ECS: camera pan/drag/zoom and hex selection. |
| [HexIcons](Assets/Presentation/HexIcons/HEXICONS.md) | A | partial | Manages per-hex UI icon badges using a UI Toolkit Screen-Space overlay. |
| [HexResourcesView](Assets/Presentation/HexResources/HEXRESOURCESVIEW.md) | A | partial | Visualizes resource entities from `HexResources` by instantiating prefabs on terrain. |
| [Presentation](Assets/Presentation/PRESENTATION.md) | A | implemented | The consolidated render/view layer: terrain mesh + water, resource visuals, and the screen-space hex-icon overlay. |
| [TerrainView](Assets/Presentation/Terrain/TERRAIN_VIEW.md) | A | implemented | Renders procedural terrain: subdivided hex mesh, isoline height fields, erosion, procedural |
| [WATER_VIEW_SETUP.md](Assets/Presentation/Terrain/WATER_VIEW_SETUP.md) | A | stub | Замінити на Uber-Stylized-Water. |
| [Context Tabs — Tab Row of the Context Sub-Panel](Assets/Presentation/UI/ContextTabs/CONTEXT_TABS.md) | A | partial | The **tab row** (Огляд / Будівлі / Дії) of the bottom panel's context sub-panel |
| [DistrictBuild](Assets/Presentation/UI/DistrictBuild/DISTRICT_BUILD.md) | A | partial | The district-build **modal overlay** (`Assets/Presentation/UI/DistrictBuild/`, namespaces |
| [Turn Corner (End Turn) — Turn Sub-Panel](Assets/Presentation/UI/EndTurn/END_TURN.md) | A | partial | The **TURN sub-panel** (left) of the shared bottom panel (`GENERAL_UI_STYLE.md` §4): the turn number «Хід N», two |
| [Hex Info Panel — Context Sub-Panel](Assets/Presentation/UI/HexInfoPanel/HEX_INFO_PANEL.md) | A | partial | The read-only **CONTEXT sub-panel** (right) of the shared bottom panel: everything known about the |
| [MainUI](Assets/Presentation/UI/MAIN_UI.md) | A | partial | The **`Presentation.UI` assembly** (`Assets/Presentation/UI/`, namespaces `Presentation.UI.*`) — the |
| [ResourceBar](Assets/Presentation/UI/ResourceBar/RESOURCE_BAR.md) | A | partial | The **left-edge resource panel**: the two inventory pools (City / Mayor) as a vertical scroll list. The |

## Canvas map (on demand)

Visual maps (Obsidian Canvas). Read/edit via Obsidian MCP; not preloaded.

| Canvas | What it maps |
|---|---|
| [DISTRICT_BUILDING_UI](DISTRICT_BUILDING_UI.canvas) | DistrictBuildUISystem |
| [ECONOMY_ACTORS](ECONOMY_ACTORS.canvas) | Ownables — each carries one OwnerFK + a Tag · My domain view · Owners — actors with an Id used as OwnerFK · Resource… |
| [WORK](WORK.canvas) | ActionTurnLeftComponent |

<!-- END GENERATED — content below is the agent zone (pass 2), preserved across runs -->

## Context & Notes (agent-maintained — pass 2)

Curate what the script can't derive: current focus, stale docs, cross-doc orientation. Keep it short. Preserved across `gen_index.py` runs.

- **Pattern recipes (`Patterns/PATTERN_*.md`)** are the granular, one-approach-per-file skeletons for the ECS
  building blocks (component / tag / event / config / config-loader / pipeline-stage / orchestrator+subsystem /
  per-frame / reactive / cleanup). **Read the matching recipe instead of opening a live system as a reference.**
  They replace the retired `SYSTEMTEMPLATE.md` / `CONFIGTEMPLATE.md` monoliths; the picker index is
  `ARCHITECTURE.md` → **Pattern Recipes**.
- **`WORK.canvas` is the user's living task-intake scratchpad** — he states tasks there as a graphic
  scheme instead of text. It always changes and contains nothing finished: never treat it as stale,
  orphaned, or a deletion candidate.
- **Policy docs in `.claude/` are OUTSIDE this map.** The index prunes dotfolders, so
  `.claude/SEARCH_POLICY.md` (the search law) is not listed here and the Obsidian MCP does not see
  it — read it via plain `Read`. The pointer to it lives in `CLAUDE.md` → Discovery Scouts.
