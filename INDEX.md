---
category: C
read: always
tags: [index, navigation]
related:
  - "[DOC_STANDARD](DOC_STANDARD.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
---

# INDEX

Generated doc map for FantasyMayor. **Do not hand-edit** — run `python3 Tools/gen_index.py` after changing any doc's frontmatter. Data source: each doc's frontmatter (`category`/`read`/`trigger`/`status`) and its first line. See `DOC_STANDARD.md`.

Totals: 34 docs — 4 always · 5 trigger · 25 reference.

## Read at start (always)

Read these every session before doing anything else.

- [FantasyMayor — Architecture Reference](ARCHITECTURE.md) — Modules are now **engine-facing / infra / UI only**. The former Hex/Terrain feature modules became the
- [CLAUDE.md](CLAUDE.md) — This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
- [DOC_STANDARD.md](DOC_STANDARD.md) — Single source of truth for how to write Markdown docs in this project.
- [FantasyMayor - Gameplay Foundation](GAMEPLAY_FOUNDATION.md) — `FantasyMayor` - це покрокова гра про управління містом через дефіцит `Action Points`, обмежені ресурси, населення як виробничу й політичну силу та нестабільний баланс влади між мером і місцевими елітами.

## Read on demand (by trigger)

Do **not** preload. Read only when the trigger condition holds.

| Doc | Read it… | What it is |
|---|---|---|
| [IAddressable Contract](Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md) | before writing/editing/reviewing Addressables, IAddressable, Box<T> or Result<T> code | Single source of truth for addressable loading. Read this; do not grep. |
| [FantasyMayor — Config Template Catalog](CONFIGTEMPLATE.md) | before working with a config, config component, or loader flow | How to create config-related classes: the authored `ScriptableObject`, its flattened ECS component, |
| [ECS_REFERENCE.md](ECS_REFERENCE.md) | before writing an ECS query, entity table/join, or adding an archetype | Central registry of ECS state in FantasyMayor: every unique entity (archetype), every world |
| [GENERAL_UI_STYLE.md](GENERAL_UI_STYLE.md) | before creating or changing UI (UI Toolkit, panels, tokens, USS) | The general UI design language for FantasyMayor: the global HUD layout model, design principles, visual |
| [FantasyMayor — System Template Catalog](SYSTEMTEMPLATE.md) | before creating or editing an ECS system or subsystem | How to create a new system. Pick the role first, then follow that role's template and rules. |

## Reference map (on demand)

Per-module navigation docs. `status` mirrors each module's `## Current State`.

| Doc | Cat | Status | What it is |
|---|---|---|---|
| [Actions](Assets/Domains/Actions/ACTIONS.md) | A | scaffold | The application / orchestration layer: actor verbs and cross-domain turn processing. Depends on both |
| [Actors](Assets/Domains/Actors/ACTORS.md) | A | partial | Game-rule domain owning actor identities **and their startup composition**. First domain under |
| [Economy](Assets/Domains/Economy/ECONOMY.md) | A | partial | Game-rule domain owning economic objects: inventory resources now; districts and buildings later. |
| [TerrainGenerator](Assets/Domains/Map/Generation/TERRAIN_GENERATOR.md) | A | implemented | Procedural terrain generation: hex grid creation, mountains with foothills, and water (river / lake / sea). |
| [HexCore](Assets/Domains/Map/Hex/HEX_CORE.md) | A | implemented | Core hex grid data structures and the per-hex terrain type. |
| [HexResources](Assets/Domains/Map/HexResources/HEXRESOURCES.md) | A | implemented | Generates logical resource data for the map. Does not render anything. |
| [Map](Assets/Domains/Map/MAP.md) | A | implemented | The world-map rule domain (bounded context): the hex grid + terrain types, procedural map generation, natural per-hex resources, and hex pathfinding. |
| [Pathfinding](Assets/Domains/Map/Pathfinding/PATHFINDING.md) | A | implemented | Hex-grid BFS pathfinding over ECS entities using native Unity collections. |
| [AxialSystem](Assets/Modules/AxialSystem/AXIAL_SYSTEM.md) | A | implemented | Hex grid coordinate system: axial math, coordinate types, and generic sparse grid storage. |
| [Boot](Assets/Modules/Boot/BOOT.md) | A | partial | Entry-point orchestration: a one-time config bootstrap, then a hand-wired game-state machine. |
| [Cameras](Assets/Modules/Cameras/CAMERAS.md) | A | implemented | Owns the shared scene-camera reference as world state, decoupled from any consumer module. |
| [Configs](Assets/Modules/Configs/CONFIGS.md) | A | implemented | Generic async loader pattern for ScriptableObject configs from Addressables. |
| [CurveBuilders](Assets/Modules/CurveBuilders/CURVE_BUILDERS.md) | A | implemented | Interface contract for building and evaluating animation curves used in terrain generation. |
| [MainCanvas](Assets/Modules/MainCanvas/MAIN_CANVAS.md) | A | implemented | Singleton provider for the main UI canvas root, behind an interface for DI. |
| [Context Tabs — Tab Row of the Context Sub-Panel](Assets/Modules/MainUI/ContextTabs/CONTEXT_TABS.md) | B | — | The **tab row** (Огляд / Будівлі / Дії) of the bottom panel's context sub-panel |
| [Turn Corner (End Turn) — Turn Sub-Panel](Assets/Modules/MainUI/EndTurn/END_TURN.md) | B | — | The **TURN sub-panel** (left) of the shared bottom panel (`GENERAL_UI_STYLE.md` §4): the turn number «Хід N», two |
| [Hex Info Panel — Context Sub-Panel](Assets/Modules/MainUI/HexInfoPanel/HEX_INFO_PANEL.md) | B | — | The read-only **CONTEXT sub-panel** (right) of the shared bottom panel: everything known about the currently |
| [MainUI](Assets/Modules/MainUI/MAIN_UI.md) | A | partial | The **Main UI module** — the game's UI-window home. Currently owns three windows: the terrain-generator |
| [Turn](Assets/Modules/Turn/TURN.md) | A | scaffold | Engine that runs a game turn: on a turn pulse it fires an ordered set of phase subsystems off the main thread and signals "a turn is being processed" so other systems can gate. |
| [UserInput](Assets/Modules/UserInput/USER_INPUT.md) | A | implemented | Bridges Unity InputSystem to ECS: camera pan/drag/zoom and hex selection. |
| [HexIcons](Assets/Presentation/Icons/HEXICONS.md) | A | partial | Manages per-hex UI icon badges using a UI Toolkit Screen-Space overlay. |
| [Presentation](Assets/Presentation/PRESENTATION.md) | A | implemented | The consolidated render/view layer: terrain mesh + water, resource visuals, and the screen-space hex-icon overlay. |
| [HexResourcesView](Assets/Presentation/Resources/HEXRESOURCESVIEW.md) | A | partial | Visualizes resource entities from `HexResources` by instantiating prefabs on terrain. |
| [TerrainView](Assets/Presentation/Terrain/TERRAIN_VIEW.md) | A | implemented | Renders procedural terrain: subdivided hex mesh, isoline height fields, erosion, procedural |
| [WATER_VIEW_SETUP.md](Assets/Presentation/Terrain/WATER_VIEW_SETUP.md) | A | stub | Замінити на Uber-Stylized-Water. |

