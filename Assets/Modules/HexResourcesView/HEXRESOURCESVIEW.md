---
category: A
read: reference
tags: [resources, terrain, view, ecs]
related:
  - "[HEXRESOURCES](../HexResources/HEXRESOURCES.md)"
  - "[TERRAIN_VIEW](../TerrainView/TERRAIN_VIEW.md)"
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
status: partial
---

# HexResourcesView

Visualizes resource entities from `HexResources` by instantiating prefabs on terrain.

## Trigger
`ForestSpawnSystem` runs on a `ForestHexAppearedEvent` pulse; `ForestDespawnSystem` on a
`ForestHexRemovedEvent` pulse (both **Reactive Systems**, Gameplay, base set = the event).
**DORMANT** — no emitter raises either pulse yet (future planting/chopping gameplay). These reactive
triggers are not visible in graphify — full event flow: `ECS_REFERENCE.md`.

## Init vs Reactive — who does what

Two different mechanisms live here, on purpose. Do not confuse them
(roles per `ARCHITECTURE.md` "System Taxonomy"):

| System | Role | Driven by | Responsibility |
|---|---|---|---|
| `HexResourcesViewSystem` | Pipeline Orchestrator (priority 400) | `MapCreation` game state | runs **Forest/Clay/Fish** view subsystems once |
| `ForestResourceViewSubSystem` | Pipeline SubSystem (priority 400) | `HexResourcesViewSystem` | **startup bulk**: plants every forest hex's trees + paints their ground, once |
| `ClayResourceViewSubSystem` | Pipeline SubSystem (priority 200) | `HexResourcesViewSystem` | sinks a clay depression into `VertexGrid` + paints a clay gradient into the terrain texture (implemented) |
| `FishResourceViewSubSystem` | Pipeline SubSystem (priority 300) | `HexResourcesViewSystem` | scaffold-only, `Update()` empty |
| `ForestSpawnSystem` | Reactive System (Gameplay) | `ForestHexAppearedEvent` pulse | reconciles forest resources **without** a view → plant them (runtime; dormant — no emitter yet) |
| `ForestDespawnSystem` | Reactive System (Gameplay) | `ForestHexRemovedEvent` pulse | reconciles views whose hex **lost** its forest resource → destroy them (runtime; dormant) |
| `ForestPlanter` | helper in `Helpers/` (not a system) | the two spawners | **stateless**: places trees as view entities + collects ground splats; the shared spawn logic |
| `ForestGroundPainter` | helper in `Helpers/` (not a system) | the spawners | **stateless**: splats green ground into the terrain texture (append-only) |
| `ClayFootprint` / `ClayDepressionShaper` / `ClayGroundPainter` | helpers in `Helpers/` (not systems) | `ClayResourceViewSubSystem` | shared contour + mesh depression + texture gradient |

**Naming marker:** `...SubSystem` = one-shot pipeline child; `...SpawnSystem`/`...DespawnSystem` =
reactive event-driven maintainers (anchored on a one-frame pulse, not per-frame diffing).
(`ForestView` = the per-tree MonoBehaviour on the prefab; `ForestViewComponent` = the ECS handle to it
— both unchanged.)

**Folder marker:** plain non-system helper classes live in `Helpers/` (painters, the clay shaper, the
footprint). ECS systems/subsystems stay in `Systems/`.

### Forest lifecycle: one-shot startup + two reactive runtime systems
Forest is split by **when** the work happens, not by a per-frame god-system:

- **Startup** — `ForestResourceViewSubSystem` runs once inside the `HexResourcesViewSystem` pipeline
  (priority 400, after Clay 200 / Fish 300 so forest splats land on top, after `TerrainViewSystem` 300
  baked the texture). It plants **all** forest hexes and paints their ground in one pass.
- **Runtime** — `ForestSpawnSystem` and `ForestDespawnSystem` are reactive `UpdatedSystem`s in
  `Gameplay`, each anchored on a one-frame **pulse** (`ForestHexAppearedEvent` / `ForestHexRemovedEvent`,
  empty marker structs + `EventTag`, disposed each tick by `EventCleanupSystem`). They are a ready
  scaffold: **no emitter raises either pulse yet** — runtime planting/chopping is future gameplay.

This replaces the old per-frame `ForestViewSyncSystem`, which diffed forest resources against forest
views *every frame* and owned both spawn and despawn. The work it did is unchanged; it now happens
on demand (pulse), not every tick.

### How the reactive systems reconcile (state, not deltas)
A pulse carries **no payload** — it only signals "forest changed". The reactive system then reconciles
against **current world state**, which makes it idempotent (a second pulse in the same frame is a no-op):

- `ForestSpawnSystem`: for each forest **resource** hex with no view (`!_forestViewsByHex.ContainsKey`),
  plant trees (`ForestPlanter`) + collect green-ground splats, then paint the batch once.
- `ForestDespawnSystem`: build the set of currently-forested hexes, then for each forest **view** hex
  **not** in that set, destroy its tree entities (+ `GameObject`s). It checks the **forest** resource
  specifically (not "any resource"). **Ground paint is left in place** — a chopped forest leaves
  vegetated ground, not bare terrain.

### Invariants shared by all forest spawning
- Each tree is a view entity (`HexIdComponent` + `ForestViewComponent`); the **ECS world is the single
  source of truth** — no mirror state is held.
- **Painting is append-only.** A patch is splatted once when its hex is planted and is never reverted, so
  `ForestGroundPainter` keeps **no baseline** and there is no rebuild — only newly planted patches are
  blended over the texture's current pixels. Not DefaultEcs `WhenAdded`/`WhenRemoved` buffers.
- Scratch uses `Unity.Collections` (`NativeHashSet`/`NativeList`, `Allocator.Temp`) disposed within the
  frame — per the ECS-system collections rule. The only managed state is the irreducible interop:
  `GameObject`/`ForestView` references. `ForestPlanter` and `ForestGroundPainter` are both **stateless**.
- Placement and prefab-pick logic (per-prefab radius/scale/tint, owned-vertex shuffle, overlap rejection)
  lives in the shared `ForestPlanter.PlantHex`, so the one-shot and the reactive spawn behave identically.

## Non-Obvious Invariants
- Resolves prefabs via `ResourceType` — does not read `Hex*ResourceTag` components.
- `HexResourcesViewConfig` **allows duplicate** `ResourceType` entries by design — all matching forest
  entries are collected, enabling random prefab selection with per-entry `Radius`, `ScaleRange`, and
  `GroundTint` (e.g. multiple tree variants with different sizes/footprints).
- `HexResourcesViewConfigEntry` carries:
  - `ScaleRange` (Vector2, x=min y=max) — uniform `localScale` chosen per instance via `Random.Range`.
  - `Radius` (float) — physical footprint of the prefab. Used both for placement rejection and as the
    green ground-splat radius.
  - `GroundTint` (Color) — color splatted under the instance; **alpha drives splat strength** (alpha 0 ⇒
    no paint).
- One forest resource entity can produce **multiple** view entities (one hex → several tree instances).
- View entities (`HexIdComponent` + `ForestViewComponent`) are separate from resource entities; on hex
  removal they are matched back by `HexIdComponent.Coords` and disposed.
- `ForestViewComponent` holds only `{ ResourceType, ForestView }` — the view MonoBehaviour reference, kept
  so the `GameObject` can be destroyed when the hex stops being a forest. No splat data is stored: paint is
  append-only and never rebuilt from the entities.
- World-space positions come from `VertexGridComponent` (`TerrainView`), not from raw hex centers.
- Tree placement iterates shuffled owned-vertex positions and skips any vertex where
  `distance < entryRadius + placedRadius` — **per-prefab radii**, not a global minimum distance.

## Ground painting (`ForestGroundPainter`)
- **Stateless and append-only.** `Paint` blends the batch of new splats directly into the texture's current
  pixels (terrain plus any earlier forest paint) and uploads once. It keeps no baseline and never reverts a
  patch — removal is not a requirement (chopped forest = vegetated ground, not bare terrain). `ComputeUvRect`
  is a pure function of the hex set; the caller recomputes it on the rare paint delta.
- World→pixel mapping (`ComputeSquareUVRect`, `WorldToPixel`) is **duplicated** from
  `TerrainViewTextureSubSystem` so splats land in the identical UV space as the baked texture. Duplication
  here is a deliberate DoD choice, not an oversight.
- Splat = colored circle with linear distance falloff, blended over existing pixels (does not overwrite
  terrain). Deterministic, no noise.

## How Clay works (`ClayResourceViewSubSystem` + `Helpers/Clay*`)
Clay has its **own** config: `Configs/ClayViewConfig` (SO) → flattened into `ClayViewConfigComponent` by
`ClayViewConfigLoaderSystem` at config load. It is **not** an entry in `HexResourcesViewConfig` — clay is
prefab-less, and that config's loader rejects entries without a prefab. Keep clay tuning out of the
forest/prefab entries.

One-shot, runs in the view pipeline (`HexResourcesViewSystem` at 400, **after** `TerrainViewSystem` at
300), so the mesh, the persistent `TerrainTextureComponent`, and the `VertexGrid` already exist. For each
clay hex it:
- builds a `ClayFootprint` — the **single source of truth** for the patch outline (ellipse via
  `FootprintAspect` → optional pear via `PearFactor` → an organic `snoise` ring). Both the geometry and
  the texture query it, so the sunk mesh and the painted clay share the exact same edge.
- `ClayDepressionShaper` lowers only that hex's `VertexGrid` vertices that fall inside the footprint, with
  a cosine bowl falloff (full depth at center → 0 at the edge). Like the isoline depressions, it only ever
  **lowers** a vertex.
- `ClayGroundPainter` blends a center→rim clay gradient into the texture, masked by the same footprint.

After the loop it re-applies heights to the mesh **once** (`TerrainView.ApplyHeightsFromVertexGrid`) and
uploads the texture **once** (`ClayGroundPainter.Apply`). Clay is persistent → never reverted.

### Non-obvious invariants
- **Noise exception.** `ClayFootprint` uses real `Unity.Mathematics.noise.snoise` — a deliberate, scoped
  exception to the project's no-noise rule, allowed **only** for cosmetic resource deformation. Terrain
  generation stays noise-free. The noise is seeded from `HexCoord`, so each clay hex differs yet stays
  stable across frames/runs.
- **Seam-safety.** Only vertices strictly inside the footprint move, and the falloff reaches 0 at the edge.
  Keep `DepressionRadius` within the hex inradius (~0.86·CellSize) so the shared boundary vertices are not
  pulled down — otherwise a seam appears with neighbours.
- **Ordering vs forest.** Both run in the same pipeline: Clay at priority 200 paints before
  `ForestResourceViewSubSystem` at 400, so forest splats land on top of clay. Clay and forest never share
  a hex (one district per hex), so they do not overlap anyway.
- **`VertexGrid.GetOwnedVertexCoords` returns the live owner-cache `HashSet`.** `VertexGrid.Set` mutates
  that same set, so you must **snapshot the coords first** (e.g. into a `NativeList`) before looping and
  calling `Set` — otherwise it throws "Collection was modified". `ClayDepressionShaper` does this; the
  forest spawn copies the coords for the same reason.
- `ClayResourceViewSubSystem` reads **world components** via `world.Get`: `TerrainViewConfigComponent`
  (CellSize), `VertexGridComponent` (depression — through the base `TryGetVertexGrid`, backed by
  `world.Has`/`world.Get`), and `TerrainTextureComponent` (paint target — also a world component, guarded
  by `world.Has`). Only `TerrainViewComponent` (mesh re-apply) is still read via an entity set, which it
  disposes in `Dispose()`. The forest subsystems (`ForestResourceViewSubSystem`/`ForestSpawnSystem`) read
  `TerrainTextureComponent` the same way — `world.Get`, no entity set.

## Current State
Forest view fully implemented end-to-end: startup spawn + append-only ground paint via the one-shot
`ForestResourceViewSubSystem`. Runtime spawn/despawn (`ForestSpawnSystem`/`ForestDespawnSystem`) is wired
and reconciliation-complete but **dormant** — no gameplay emitter raises the pulses yet. Ground paint is
intentionally not reverted on removal.
**Clay implemented** — organic depression in `VertexGrid` + clay-palette texture gradient (one-shot
pipeline). Fish subsystem is scaffold-only — `Update()` is empty.
