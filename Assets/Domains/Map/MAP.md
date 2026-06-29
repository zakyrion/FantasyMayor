---
category: A
read: reference
tags: [map, domain, hex, terrain, ecs]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[HEX_CORE](Hex/HEX_CORE.md)"
  - "[TERRAIN_GENERATOR](Generation/TERRAIN_GENERATOR.md)"
  - "[HEXRESOURCES](HexResources/HEXRESOURCES.md)"
  - "[PATHFINDING](Pathfinding/PATHFINDING.md)"
status: implemented
---

# Map

The world-map rule domain (bounded context): the hex grid + terrain types, procedural map generation, natural per-hex resources, and hex pathfinding.

## Purpose
`Domains.Map` owns the spatial substrate of the game — everything that *is* the world, as data and
rules. It is **pure data + logic with no view/render dependency**: the `Presentation` layer reads this
domain to draw it, never the reverse.

## Layout (one asmdef `Domains.Map`, feature sub-areas)
- `Hex/` (`Domains.Map.Hex.*`) — hex entity table, `HexIdComponent` (the universal hex key),
  `HexType` + `HexTypeComponent`, `VertexGrid` and hex utilities.
- `Generation/` (`Domains.Map.Generation.*`) — procedural map generation: `MapGenerationSystem`
  orchestrator + Mountain/River/Lake/Sea subsystems, on the `MapGenerationStep` world-init pipeline.
  Config types keep the `TerrainGeneration*` names (their addressable keys are unchanged).
- `HexResources/` (`Domains.Map.HexResources.*`) — natural per-hex resource layer (Forest/Clay/Fish),
  distinct from `Domains.Economy`'s inventory resources.
- `Pathfinding/` (`Domains.Map.Pathfinding.*`) — hex-grid BFS over ECS entities (+ its installer).
  Folded in here because `Generation` depends on it and it depends on the hex table (avoids a cycle).

## Non-Obvious Invariants
- **`AxialSystem` stays a shared kernel module** (coordinate math) that this domain depends on — it is
  not part of `Domains.Map`.
- Cross-domain consumers read this domain one-way: `Economy → Map` (districts gate on hex type),
  `Presentation → Map` (rendering). Visible in the ecs-graph (`/ecs-graph`, cross-module component reads).

## Current State
Implemented. Consolidated from the former `HexCore`, `TerrainGenerator`, `HexResources`, and
`Pathfinding` modules. Archetypes and components: see the ecs-graph (`/ecs-graph`).
