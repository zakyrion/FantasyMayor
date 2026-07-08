---
category: A
read: reference
tags: [presentation, view, render, ecs]
related:
  - "[ARCHITECTURE](../../ARCHITECTURE.md)"
  - "[TERRAIN_VIEW](Terrain/TERRAIN_VIEW.md)"
  - "[HEXRESOURCESVIEW](HexResources/HEXRESOURCESVIEW.md)"
  - "[HEXICONS](HexIcons/HEXICONS.md)"
  - "[MAIN_UI](UI/MAIN_UI.md)"
status: implemented
---

# Presentation

The consolidated render/view layer: terrain mesh + water, resource visuals, and the screen-space hex-icon overlay.

## Purpose
`Presentation` is the entire view tier in one assembly. It **reads the rule domains it draws** (chiefly
`Domains.Map`) and renders them; **domains never depend on it**. Game mechanics never touch views — this
is the layered (DDD-strategic) boundary described in `ARCHITECTURE.md`.

## Layout (asmdefs `Presentation` + `Presentation.UI`, feature sub-areas)
- `Terrain/` (`Presentation.Terrain.*`, was `Terrain.View`) — terrain mesh, textures, water view,
  isolines, smoothing, runtime view systems. Terrain-transition pre-read files live here.
- `HexResources/` (`Presentation.HexResources.*`, was `Resources/`) — forest/clay/fish views and ground
  painting. Renamed from `Resources/` to avoid the magic Unity folder name and economy-`Resource` confusion.
- `HexIcons/` (`Presentation.HexIcons.*`, was `Icons/`) — screen-space per-hex icon overlay (UI Toolkit).
- `UI/` (own asmdef `Presentation.UI`) — the screen-space HUD half of the presentation tier and the
  game's UI-window home; relocated from the former `MainUI` module. See `MAIN_UI.md`.
- `Districts/` — district view spawn/config (`DistrictsInstaller`). See `Districts/DISTRICTS.md`.

## Non-Obvious Invariants
- Single assembly-group is deliberate: the terrain ↔ resource-view ↔ icon coupling (e.g. resource views
  read terrain texture/cell-size) is **intra-layer** for `Presentation` itself, which is why the old
  cross-module cycle disappeared; `Presentation.UI` is a second asmdef layered on top of `Presentation`.
- Depends one-way on the domain layers it renders (full list: `Presentation.asmdef` /
  `Presentation.UI.asmdef`). Adding a reverse edge (a domain referencing `Presentation`) is forbidden —
  it would reintroduce a cycle.

## Current State
Implemented. Consolidated from the former `TerrainView`, `HexResourcesView`, and `HexIcons` modules.
View systems and components: see the ecs-graph (`/ecs-graph`) (attributed to `Presentation/...`).
