# HexResourcesView

Visualizes resource entities from `HexResources` by instantiating prefabs on terrain.

## Init vs Reactive — who does what

Two different mechanisms live here, on purpose. Do not confuse them:

| System | Kind | Driven by | Responsibility |
|---|---|---|---|
| `HexResourcesViewSystem` | one-shot pipeline step (priority 400) | `MapCreation` game state | runs **Clay/Fish** view subsystems once |
| `ClayResourceViewSubSystem` | pipeline subsystem (priority 200) | `HexResourcesViewSystem` | sinks a clay depression into `VertexGrid` + paints a clay gradient into the terrain texture (implemented) |
| `FishResourceViewSubSystem` | pipeline subsystem | `HexResourcesViewSystem` | scaffold-only, `Update()` empty |
| `ForestViewSyncSystem` | reactive `UpdatedSystem` (per-frame) | Boot per-frame loop | owns the **whole forest view lifecycle** |
| `ForestGroundPainter` | helper in `Helpers/` (not a system) | `ForestViewSyncSystem` | **stateless**: splats green ground into the terrain texture (append-only) |
| `ClayFootprint` / `ClayDepressionShaper` / `ClayGroundPainter` | helpers in `Helpers/` (not systems) | `ClayResourceViewSubSystem` | shared contour + mesh depression + texture gradient |

**Naming marker:** `...SubSystem` = one-shot pipeline child; `...SyncSystem` = reactive per-frame
maintainer. (`ForestView` = the per-tree MonoBehaviour on the prefab; `ForestViewComponent` = the ECS
handle to it — both unchanged.)

**Folder marker:** plain non-system helper classes live in `Helpers/` (painters, the clay shaper, the
footprint). ECS systems/subsystems stay in `Systems/`.

### Why forest is reactive (and Clay/Fish are not, yet)
The one-shot pipeline builds the view exactly once. That cannot reflect **runtime** changes — a chopped
forest hex would leave its trees on the map forever, and ground paint contributed after the bake would
never appear. `ForestViewSyncSystem` fixes both by maintaining the view every frame. Clay/Fish stay
one-shot scaffolds until they need the same treatment.

There used to be a one-shot `ForestResourceViewSubSystem`; it was **deleted** and its spawn logic moved
into `ForestViewSyncSystem`, so there is a single source of truth for forest spawning.

## How `ForestViewSyncSystem` works
- Based on (and gated by) the **persistent** `TerrainTextureComponent` — it is both the paint target and
  the readiness signal. No texture ⇒ the system idles (its base set is empty).
- **The ECS world is the single source of truth — the system holds no mirror state.** Each tree is a view
  entity (`HexIdComponent` + `ForestViewComponent`). Every frame it diffs the **forest resource hexes**
  against the **forest-view hexes**:
  - hex appeared (resource, no view) ⇒ spawn trees + paint their green ground patches (once)
  - hex vanished (view, no resource) ⇒ destroy that hex's view entities (+ their `GameObject`s). **Ground
    paint is left in place** — a chopped forest leaves vegetated ground, not bare terrain.
- **Painting is append-only.** A patch is splatted once when its hex appears and is never reverted, so the
  painter keeps **no baseline** and there is no rebuild. Only the **newly spawned** patches are painted each
  delta, blended over the texture's current pixels.
- **Reactivity is per-frame diffing**, the same idiom as `TerrainView`'s `HexSelectionViewSystem` — *not*
  DefaultEcs `WhenAdded`/`WhenRemoved` reactive buffers.
- Per-frame scratch uses `Unity.Collections` (`NativeHashSet`/`NativeList`, `Allocator.Temp`) disposed
  within the frame — per the ECS-system collections rule. The only managed state is the irreducible interop:
  `GameObject`/`ForestView` references. The painter itself is **stateless**.
- Painting runs **only on an appeared-delta**, never every frame. A frame with no new forest does a cheap
  diff and returns.

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
- **Ordering vs forest.** Clay paints the texture during the pipeline, before `ForestViewSyncSystem`
  captures its baseline (first Gameplay frame). So clay is part of that baseline and forest splats land on
  top. Clay and forest never share a hex (one district per hex), so they do not overlap.
- **`VertexGrid.GetOwnedVertexCoords` returns the live owner-cache `HashSet`.** `VertexGrid.Set` mutates
  that same set, so you must **snapshot the coords first** (e.g. into a `NativeList`) before looping and
  calling `Set` — otherwise it throws "Collection was modified". `ClayDepressionShaper` does this; the
  forest spawn copies the coords for the same reason.
- `ClayResourceViewSubSystem` reads the cross-module config `TerrainViewConfigComponent` (CellSize) as a
  **world component** via `world.Get`, and reads runtime singletons via entity sets:
  `TerrainTextureComponent` (paint target), `TerrainViewComponent` (mesh re-apply), `VertexGridComponent`
  (depression). It disposes its own extra entity sets in `Dispose()`.

## Current State
Forest view fully implemented end-to-end and reactive (spawn + append-only ground paint + tree removal;
ground paint is intentionally not reverted).
**Clay implemented** — organic depression in `VertexGrid` + clay-palette texture gradient (one-shot
pipeline). Fish subsystem is scaffold-only — `Update()` is empty.
