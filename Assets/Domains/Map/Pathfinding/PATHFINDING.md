---
category: A
read: reference
tags: [pathfinding, hex, ecs]
related:
  - "[TERRAIN_GENERATOR](../Generation/TERRAIN_GENERATOR.md)"
status: implemented
code_refs:
  types: [HexPathfindingUtility, HexCoord]
---

# Pathfinding

Hex-grid BFS pathfinding over ECS entities using native Unity collections.

## How To Use Correctly
- `TryFindPath` returns `true` and fills an out `NativeList<HexCoord>` when a path exists.
- **The caller owns disposal.** Every native container the utility produces uses the
  `Allocator` you pass in — you must dispose the result list yourself.
- The search domain is built from `hexSet` entities that carry `HexIdComponent`.
  A hex not in that set is treated as impassable / non-existent.

## Design Decisions
- `HexPathfindingUtility` is **stateless** — safe to reuse across calls; it holds no per-search state
  between invocations. (Registered as a singleton: `dig.py explain HexPathfindingUtility`.)

- Layout deviation: DI registration lives in a **separate installer assembly** exposing the module's
  `IInstaller` — not a module-local `Installer/` folder inside the runtime assembly.

## Current State
Stable. Plain BFS (no weighting / diagonal cost).
