---
category: A
read: reference
tags: [hex, math, grid]
related:
  - "[HEX_CORE](../HexCore/HEX_CORE.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
status: implemented
---

# AxialSystem

Hex grid coordinate system: axial math, coordinate types, and generic sparse grid storage.

## Purpose
The geometric foundation for the whole map. Everything that places things on hexes
(terrain, resources, selection) converts through here.

## How To Use Correctly
- **Hex → world position.** To place anything over a tile, convert its `HexCoord` directly:
  `AxialMath.AxialToWorld(coord.Value, cellSize, AxialOrientation.PointyTop)` returns the tile centre in
  world space. `cellSize` comes from `TerrainViewConfigComponent.CellSize`. This is the single canonical
  recipe — used by `TerrainViewDebugSystem`, `ClayHexResourceViewSubSystem`, etc.
- **Never reconstruct the centre from `VertexGrid`.** That grid is the FlatTop fine-mesh geometry; a
  centroid of its vertices is the wrong tool for a tile centre and duplicates math that already exists here.

## Non-Obvious Invariants
- Two orientations exist and they are not interchangeable:
  - **PointyTop** → the coarse tile grid (`HexGrid`). One cell per game hex.
  - **FlatTop** → the fine vertex grid (`VertexGrid`) used for mesh geometry.
- `HexCoord` and `VertexCoord` are structurally identical (both wrap an `int2`) but
  semantically different: one addresses a tile, the other a mesh vertex. Do not mix them.
- Both coord types are unmanaged `readonly struct` — safe inside `NativeArray` / `stackalloc`.
- `AxialGrid<TCoord, T>` is **sparse** (dictionary-backed), not a dense array.
  Empty cells cost nothing; iteration only visits set cells.
- `MinCoord` / `MaxCoord` are recomputed on every `Set` — reading them is free, but they
  only reflect cells that have been set.
- `Get` throws if the cell is absent. Use `TryGet` when absence is possible.

## Design Decisions
- Concrete grid subclasses (`HexGrid`, `VertexGrid`) live in `HexesCore`, not here.
  `AxialSystem` stays a pure-math, dependency-light foundation (`Unity.Mathematics` only).

## Current State
Stable. Used by all spatial modules.
