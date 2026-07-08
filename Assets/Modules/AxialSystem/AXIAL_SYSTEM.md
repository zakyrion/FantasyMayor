---
category: A
read: reference
tags: [hex, math, grid]
related:
  - "[HEX_CORE](../../Domains/Map/Hex/HEX_CORE.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
status: implemented
code_refs:
  types: [HexCoord, VertexCoord, AxialMath, AxialGrid, VertexGrid]
  enums: [AxialOrientation]
---

# AxialSystem

Hex grid coordinate system: axial math, coordinate types, and generic sparse grid storage.

## Purpose
The geometric foundation for the whole map. Everything that places things on hexes
(terrain, resources, selection) converts through here.

## How To Use Correctly
- **Hex → world position.** To place anything over a tile, convert its `HexCoord` directly via
  `AxialMath`: `AxialToWorld(coord.Value, cellSize, orientation)` returns the full 3D tile centre
  (height-aware; used by `TerrainViewDebugSystem`), while `AxialToWorld2D(coord.Value, cellSize)`
  returns the XZ centre only — the more common entry point, used by `TerrainView`, `WaterView`,
  `ClayHexResourceViewSubSystem`, and the ground painters. `cellSize` comes from
  `TerrainViewConfigComponent.CellSize`.
- **Never reconstruct the centre from `VertexGrid`.** That grid is the FlatTop fine-mesh geometry; a
  centroid of its vertices is the wrong tool for a tile centre and duplicates math that already exists here.

## Non-Obvious Invariants
- Two orientations exist and they are not interchangeable:
  - **PointyTop** → the coarse tile grid. No dedicated grid class wraps it — coarse hex
    data is passed around as a plain `NativeArray<HexCoord>` (e.g. `VertexGrid.BuildVertices`).
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
- The concrete grid subclass (`VertexGrid`) lives in the `Hex` domain (`HEX_CORE.md`), not here.
  `AxialSystem` stays a pure-math, dependency-light foundation (`Unity.Mathematics` only).

- Layout deviation: flat utility-style module (no `Components/Systems/…` feature folders) —
  deliberate for a shared kernel.

## Current State
Stable. Used by all spatial modules.
