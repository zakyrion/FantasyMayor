# TerrainView

Renders procedural terrain: subdivided hex mesh, isoline height fields, erosion, procedural
texture, water, and the hex-selection highlight.

## Pre-read Before Modifying Transitions
Terrain height transitions are subtle. Read these first:
- `Isolines/FieldBasedIsolineBuilder.cs`
- `Isolines/IsolineSlopeTransition.cs`
- `Smooth/HeightSmoothing.cs`
For water: `WATER_VIEW_SETUP.md`.

## Non-Obvious Invariants

### How TerrainView is triggered
- `TerrainViewSystem` is a **Pipeline Orchestrator** (world-init stage, priority 300) — an
  `IPrioritizedUniTaskSystem<TerrainGenerationStep>` run sequentially by the **`MapCreation` game state**,
  after terrain and resource generation. It fans out into async **Pipeline SubSystems**
  (`ViewSubSystem` children: height field, texture, water) and contains no domain logic of its own.
  It no longer listens to an event itself.
- The pipeline runs once when the `MainMenu` state detects `TerrainGenerationGenerateEventComponent`
  (Generate button) and switches to `MapCreation`. Stage order: terrain gen (100) → resources (200) →
  terrain view (300) → resource view (400) → selection-view load (500) → debug (600) →
  icon containers (700) → info panel (800).
- The `TerrainGenerationStep` marker drives this pipeline.
- This module also owns two later pipeline stages: `HexSelectionViewLoadingSystem` (500) and
  `TerrainViewDebugSystem` (600). `HexSelectionViewSystem` (the selection highlight) is a
  **Per-frame System** (`UpdatedSystem`), wired by Boot into the `Gameplay` state — **not** part of
  the pipeline.
- Roles: `ARCHITECTURE.md` "System Taxonomy". Full flow: `BOOT.md` / `ECS_REFERENCE.md`.

### Async build pipeline order (contract)
`TerrainViewSystem` runs this sequence; the order is significant:
1. Load the TerrainView prefab (Addressables).
2. Build the base mesh from hex coords.
3. Run view subsystems in priority order: **height field (100) before texture (200)**.
4. Apply the generated texture.
5. Apply heights from the shared `VertexGrid`.

### Lifetimes and ownership
- `VertexGridComponent` is a **world component** (`world.Set` / `world.Get`, not an entity), set once by
  `TerrainViewConfigLoaderSystem` at config load. The grid is shared mutable state — the height subsystem
  mutates it in place. Because the grid is a reference type, `world.Set` is called only at creation and
  never re-called after a mutation; readers `world.Get` the live grid (guarded by `world.Has`, fail-loud).
- `TerrainTextureComponent` is a **world component** (`world.Set` / `world.Get`, not an entity), set once
  by `TerrainViewTextureSubSystem` in the view pipeline; `TerrainViewSystem` `world.Get`s it and applies
  its `Texture2D` to the material. It is **persistent** — never disposed. The same `Texture2D` instance
  stays on the material, so reactive runtime systems can mutate its pixels (`SetPixels32` + `Apply`) and
  the material reflects the change without re-applying. The texture is created readable (`Apply(false)`)
  precisely so it can be read back and repainted later. Like `VertexGridComponent`, the carried `Texture2D`
  is a reference type, so `world.Set` happens only at creation, never after a paint mutation.
  Current consumers: `HexResourcesView`'s `ClayGroundPainter` (clay gradient) and `ForestGroundPainter`
  (green ground under trees).
- `HexSelectionView.ShowSelectionBorder` receives `Allocator.Temp` NativeArrays. **The caller
  disposes them after the call** — the view does not take ownership.

### Selection highlight geometry
- The outer ring is the hex's **boundary vertices** (shared by more than one hex, or with fewer
  than 6 neighbors). The inner ring is found by BFS stepping inward from the outer ring.
  The highlight is a strip mesh between the two rings.

## Public Contract & Gotchas
- `TerrainView.ApplyHeightsFromVertexGrid(grid)` is **safe to call again after the initial bake**. It
  re-reads the whole mesh and rewrites each vertex `y` from the grid, then recalculates normals/bounds —
  so any post-bake grid edit (e.g. clay depressions in the resource-view stage at 400) is picked up by
  calling it once more. Nothing later in the pipeline re-applies heights, so such edits persist.
- It maps mesh vertices to the grid by `WorldToAxial(x, 0, z)` — **only XZ matters**. Changing just
  `HexVertex.Position.y` in the grid is enough; you do not need to touch X/Z.
- `TerrainView.ApplyTexture(texture)` **takes ownership** of the `Texture2D` — it is destroyed when the
  view is disposed. Do not also destroy it yourself.

## Current State
Stable. Mesh, height field, erosion, texture, water, and selection highlight all working.
