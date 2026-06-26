---
category: A
read: reference
tags: [presentation, view, render, ecs]
related:
  - "[ARCHITECTURE](../../ARCHITECTURE.md)"
  - "[ECS_REFERENCE](../../ECS_REFERENCE.md)"
  - "[TERRAIN_VIEW](Terrain/TERRAIN_VIEW.md)"
  - "[HEXRESOURCESVIEW](Resources/HEXRESOURCESVIEW.md)"
  - "[HEXICONS](Icons/HEXICONS.md)"
status: implemented
---

# Presentation

The consolidated render/view layer: terrain mesh + water, resource visuals, and the screen-space hex-icon overlay.

## Purpose
`Presentation` is the entire view tier in one assembly. It **reads the rule domains it draws** (chiefly
`Domains.Map`) and renders them; **domains never depend on it**. Game mechanics never touch views — this
is the layered (DDD-strategic) boundary described in `ARCHITECTURE.md`.

## Layout (one asmdef `Presentation`, feature sub-areas)
- `Terrain/` (`Presentation.Terrain.*`, was `Terrain.View`) — terrain mesh, textures, water view,
  isolines, smoothing, runtime view systems. Terrain-transition pre-read files live here.
- `HexResources/` (`Presentation.HexResources.*`, was `Resources/`) — forest/clay/fish views and ground
  painting. Renamed from `Resources/` to avoid the magic Unity folder name and economy-`Resource` confusion.
- `HexIcons/` (`Presentation.HexIcons.*`, was `Icons/`) — screen-space per-hex icon overlay (UI Toolkit).

## Non-Obvious Invariants
- Single assembly is deliberate: the terrain ↔ resource-view ↔ icon coupling (e.g. resource views read
  terrain texture/cell-size) is now **intra-layer**, which is why the old cross-module cycle disappeared.
- Depends one-way on `Domains.Map` (+ `Cameras`, `CurveBuilders`, `Unity.AppUI`). Adding a reverse edge
  (a domain referencing `Presentation`) is forbidden — it would reintroduce a cycle.

## Current State
Implemented. Consolidated from the former `TerrainView`, `HexResourcesView`, and `HexIcons` modules.
View systems and components: see `ECS_REFERENCE.md` (attributed to `Presentation/...`).
