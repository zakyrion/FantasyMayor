---
category: A
read: reference
tags: [hex, ecs, terrain]
related:
  - "[AXIAL_SYSTEM](../AxialSystem/AXIAL_SYSTEM.md)"
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
status: implemented
---

# HexCore

Core hex grid data structures and terrain tag components.

## Purpose
Holds the runtime data the rest of the game builds on: the fine `VertexGrid` (mesh geometry),
`HexVertex` / `HexData`, hex utility math, and the terrain-type tag components.

## Public Contract & Gotchas

### VertexGrid mutation (the contract behind the API)
- `GetOwnedVertexCoords(hex)` returns the **live** owner-cache `HashSet`, **not a copy**.
- `Set(coord, vertex)` has a **side-effect**: it re-runs the owner-cache bookkeeping
  (remove + re-add the coord), even when the owners did not change.
- **Common mistake:** iterating `GetOwnedVertexCoords(hex)` while calling `Set` inside the loop throws
  `Collection was modified`. **Snapshot the coords first** (e.g. into a `NativeList<VertexCoord>`), then
  loop the snapshot and call `Set`. `ClayDepressionShaper` and the forest spawn both do this.
- To edit a vertex height: `Get(coord)` → change `Position.y` → `Set(coord, vertex)`. The grid is a
  **class** (reference) singleton, so the mutation is visible to every system that reads it (e.g. the
  terrain mesh re-apply).
- `BuildVertices` calls `Clear()` and rebuilds from scratch; `Remove(coord)` also updates the owner cache.

## Non-Obvious Invariants

### VertexGrid
- It is the **fine** grid (flat-top), one `HexVertex` per subdivided vertex — distinct from the
  coarse tile grid. Registered as a `Lifetime.Singleton`; treat it as shared mutable state.
- `BuildVertices` expands each coarse hex outward by `subdivisions` BFS waves. Vertices on a hex
  boundary end up **owned by several hexes** — this is intended, not a bug.

### HexVertex owner semantics
The owner count is the meaning, not just a number:
- `0` → ghost vertex (no owning hex)
- `1` → interior vertex (unique to one hex)
- `2` → edge vertex (shared by two hexes)
- `3` → corner vertex (shared by three hexes)
- `AddOwner` throws if a fourth owner is added — a vertex can never have more than 3.
- `MeshIndex == -1` means the vertex has no mesh counterpart.

### Terrain tag components (data-less markers)
These carry no fields; the tag *is* the meaning. Present on hex entities:
- `HexPlainTag` → flat terrain (plain, Level 0)
- `HexMountTag` → part of a mountain body
- `HexBedhillTag` → part of a foothill band
- `HexWaterTag` → water

## Current State
Stable. Core data layer — read by almost every terrain and resource system.
