---
category: A
read: reference
tags: [hex, ui, icons, ecs]
related:
  - "[HEXRESOURCES](../../Domains/Map/HexResources/HEXRESOURCES.md)"
  - "[ADDRESSABLE_PATTERNS](../../Modules/Addressable/ADDRESSABLE_PATTERNS.md)"
status: partial
code_refs:
  systems:          [HexIconsConfigLoaderSystem, HexIconsSpawnSystem, HexIconsVisibilitySystem, HexIconsContainerPositionSystem]
  components:       [HexIdComponent, HexIconContainerComponent]
  world_components: [HexIconsViewComponent, HexIconsConfigComponent, HexResourceIconConfigComponent, HexIconsVisibilityComponent]
  events:           [HexIconsVisibilityChangedEvent]
  tags:             [HexTag]
  types:            [HexIconsView, ResourceIconEntry]
  configs:          [HexIconsConfig, HexResourceIconConfig]
---

# HexIcons

Manages per-hex UI icon badges using a UI Toolkit Screen-Space overlay.

## Trigger
`HexIconsVisibilitySystem` runs on a `HexIconsVisibilityChangedEvent` pulse (a **Reactive System**,
Gameplay — base set = the event; the truth lives in the `HexIconsVisibilityComponent` world
component). Producer today: `GameplayState.EnterAsync`; later a UI toggle. This reactive trigger is
not visible in roslyn-mcp — full event flow: the ecs-graph (`/ecs-graph`).

System roles + priorities: `mcp__ecs-graph__system_contract <System>` (roles per `ARCHITECTURE.md`
"System Taxonomy").

## Non-Obvious Invariants
- `HexIconsConfigComponent` owns the `Box<HexIconsConfig>` — the loader transfers ownership on Set and
  nulls the local. This deviates from the standard ADDRESSABLE_PATTERNS.md rule ("loader retains Box").
  Consequence: there is no explicit disposal path for the Box when the world tears down (acceptable for a
  singleton config that lives for the full application lifetime).
- `HexResourceIconConfigComponent` follows the same Box-ownership deviation: it owns the
  `Box<HexResourceIconConfig>`, the loader nulls its local after `World.Set`.
- **`HexResourceIconConfig` and `HexResourceIconConfigComponent` are `public` and shared.** The HexesUI info
  panel (`HexInfoPanelResourcesSystem`) reads the same `HexResourceIconConfigComponent` world component to
  render its resource chips. Treat these two types as a read-only cross-module contract — changing their
  shape affects HexesUI as well.
- One loader (`HexIconsConfigLoaderSystem`) loads **both** configs. The two boxes stay owned inside a
  single try/finally until every `World.Set` has run; both locals are nulled only after the last Set.
  So a failure/cancel on the second load rolls back the first via the shared `finally` (DisposeBox) —
  no half-committed state, no leak.

## Non-Obvious Invariants (Spawn)
- **Screen-space overlay.** The prefab is a Screen-Space UI Toolkit document (configured prefab-side). The
  spawn system does **not** touch a `WorldDocumentRaycaster`, the instance `transform`, or the root pixel
  size — the panel covers the screen via its own `PanelSettings`. Spawn instantiates the prefab, grabs
  `HexIconsView`, makes the overlay raycast-transparent, caches `_view`, sets the view component, and
  **creates one empty container entity per hex** (`CreateContainers`). It does **not** add any icons — icon
  rendering is the event-driven `HexIconsVisibilitySystem`'s job (Gameplay). It throws (and destroys the
  instance) if the prefab has no `HexIconsView`, and throws if `HexIconsConfigComponent` or the prefab is
  missing. Spawn no longer reads the camera, the `VertexGrid`, or `HexResourceIconConfigComponent`.
- `HexIconsViewComponent` is a world component (not an entity). Read via `world.Get<HexIconsViewComponent>()`.
- **Raycast transparency:** the `hex-icons-root` element carries the `raycast-transparent` marker class.
  Spawn calls `HexIconsView.ApplyRaycastTransparent()`, which queries the document root for that class and
  calls `EnablePicking(false)` (Unity.AppUI extension) — mirrors the `HexesUI` convention. The USS rule is
  an **empty marker** (`.raycast-transparent {}`); picking is disabled in code, not via USS. Without this
  the full-screen overlay would swallow every pointer ray and block world/hex selection. Requires the
  `Unity.AppUI` assembly reference.
- **Every element in the overlay is raycast-transparent**, not just the root. Picking does **not** propagate
  to children in UI Toolkit, so each runtime-created element is marked individually via
  `HexIconsView.MakeRaycastTransparent` (adds the marker class + `EnablePicking(false)`): per-hex containers
  when created (spawn), and each icon when added (`HexIconsVisibilitySystem.AddIcon`). Both are created
  *after* the spawn-time `ApplyRaycastTransparent` pass (which only covers UXML-authored elements like
  `hex-icons-root`), so they must be marked explicitly. Net effect: overlay, containers, and icons all let
  pointer rays pass through.
- **Per-hex container entity (`CreateContainers` + `CreateContainerElement(int2)`):** spawn **eagerly**
  creates one container `VisualElement` (`hex-container-{q}-{r}`) for **every** hex — there is **no
  dictionary**; the container store is the ECS entity set. For each hex (selected via `With<HexTag>`, the
  hex-only marker — `HexIdComponent` alone would also match the resource / resource-view parallel tables)
  it builds the element and creates a **parallel container entity** carrying `HexIdComponent` (the FK,
  copied from the hex) + `HexIconContainerComponent` (the managed `VisualElement` ref). The element is
  **auto-fit** (no fixed size — sizes to its icons), `Position.Absolute`, a centered vertical list
  (`flexDirection: Column`, `alignItems: Center`), and **self-centers on its anchor** via
  `translate: -50% -50%` (percentage translate). Spawn does **not** position it — that is the position
  system's job.
## Non-Obvious Invariants (Visibility / icon rendering)
- **State (variant B): `HexIconsVisibilityComponent`** is a mutable **world** component (`bool IsVisible`) —
  the single source of truth for whether icons are shown. It is **public** so the producer (Boot assembly)
  can write it. The *producer writes the state, then raises the event*; the consumer only reads the state.
- **Event: `HexIconsVisibilityChangedEvent`** is a **one-frame, payload-less signal** ("re-render now"). It
  lives on an entity tagged with `EventTag` and is disposed by `EventCleanupSystem` (priority `int.MaxValue`)
  at end of tick. Producer pattern: `Set(new HexIconsVisibilityChangedEvent())` + `Set(new EventTag())` on a
  fresh entity (mirrors `HexesUI`). **Producer (for now):** `GameplayState.EnterAsync` sets the state
  (`IsVisible = true`) and raises one event on entering Gameplay; later a UI control does the same on toggle.
- **Consumer: `HexIconsVisibilitySystem`** (`UpdatedSystem`, Gameplay, priority 800). Its base set **is the
  event set** (`With<HexIconsVisibilityChangedEvent>`), so `Update` only fires on a frame an event exists —
  one-frame events are cleaned up each tick, so the set only ever holds *just-added* events. Zero idle cost.
  On an event it resolves prerequisites **fail-loud** (`HexIconsVisibilityComponent`, `HexIconsViewComponent`,
  `HexIconsConfigComponent`, `HexResourceIconConfigComponent`), then iterates **all** container entities:
  `container.Clear()`, and when `IsVisible` rebuilds each from that hex's resources. **No render state is
  cached** — every event is a full clear-and-rebuild.
- **Rebuild = data-driven from real resources.** For each container, the system scans the resource table
  (`With<HexIdComponent>().With<HexResourcesComponent>()`), matches `Coords`, and for each matched
  `ResourceType` does a **linear** `TryFindSprite` over `HexResourceIconConfig.Entries` (Try + out + bool).
  A missing entry **or** a null sprite on the entry is a **skip**, not an error — not every resource has art.
  Found → `AddIcon` (fixed `IconSize × IconSize`, `flexShrink: 0` flow child, individually raycast-transparent).
  The container×resource scan is O(C×R) but runs only on event frames.

## Non-Obvious Invariants (Per-frame positioning)
- **`HexIconsContainerPositionSystem` is a per-frame `LateUpdatedSystem`** over the
  `With<HexIconContainerComponent>` set, registered as a concrete singleton and wired into the **Gameplay**
  state by `Boot`. It re-projects every container each frame so positions track the camera (replacing the
  old one-shot spawn projection). **It runs in LateUpdate, priority 700 — after `CameraMovementSystem`
  (LateUpdate, priority 0).** Running it in `Update` instead lags the camera by one frame (positions are
  computed before the camera moves that frame), so the icons visibly slide while panning/zooming.
- **Per-frame resolve in `PreUpdate`, per-entity project in `Update`.** `PreUpdate` resolves the panel
  (`HexIconsViewComponent.View.Root.panel`), the camera + its `ReferenceFieldOfView` (`CameraComponent`), the
  `VertexGrid` (world component `VertexGridComponent`), and the `HexIconsConfigComponent` (for `WorldYOffset`)
  once per frame, computes the resolved zoom scale + focus depth, and packs everything into a single
  frame-stamped `FrameBox<FramePose>` (`Core`); it **throws** (fail-loud) if any is missing — by Gameplay the
  spawn step has run, so absence is a real error.
- **The system holds NO cross-frame state.** Its only instance field is the `FrameBox` (marked
  `[StateAllowed]`): filled in `PreUpdate`, valid for that frame only — a stale `.Value` in a later frame
  **throws** instead of reading outdated data. `Dispose` drops the box (clearing the held Camera/grid/panel
  refs) plus the secondary `_vertexGridSet`. There is **no latched zoom reference** — see the scale bullet.
- **Projection convention.** Per entity: read `HexIdComponent.Coords` → the hex's **real on-mesh center**
  from the `VertexGrid` (`GetCenterVertexCoord` → `TryGet` → `HexVertex.Position`, a `float3` whose Y is the
  actual terrain height — the **same geometry the selection outline uses**, so X/Z *and* height match what
  is rendered; a flat `y = 0` point caused a diagonal offset under the tilted camera). The center is then
  **lifted by `HexIconsConfig.WorldYOffset`** (world-space `+Y`, default `1.5`) so the icon floats above the
  hex; the lift is in world space, so it foreshortens with perspective and the **lifted** point's depth feeds
  both the position and the per-hex scale. `WorldYOffset` is read per frame in `PreUpdate` (fail-loud on a
  missing `HexIconsConfigComponent`), so inspector tweaks apply live in Play. Then
  `Camera.WorldToScreenPoint` (bottom-left origin, Y up) → flip Y with **`Screen.height − y`** →
  `RuntimePanelUtils.ScreenToPanel` (expects top-left-origin screen Y, does **not** flip itself — Unity's
  documented world-anchored-UI pattern; omitting the flip mirrors placement vertically). A hex missing from
  the grid, or `z ≤ 0` (behind the camera), → container `display: None`.
- **Still `left/top`, runs unconditionally.** Positioning sets `style.left/top` every frame (the `-50% -50%`
  translate set at creation still does the centering) and re-projects on **every** `LateUpdate` — no
  camera-moved gate yet.
- **Per-hex perspective scale (`ComputeZoomScale` + per-entity factor).** Final `style.scale` per container is
  `zoomScale * (focusDepth / hexDepth)`:
  - **`zoomScale`** — the map-wide **zoom baseline**, computed **analytically** from FOV in `ComputeZoomScale`:
    `tan(ReferenceFieldOfView/2) / tan(Camera.fieldOfView/2)`. Zoom is FOV-driven (`CameraMovementSystem`
    changes `fieldOfView`, not camera distance), so the on-screen size of a fixed world span ∝ `1/tan(fov/2)`.
    `ReferenceFieldOfView` is the camera's startup FOV (read from `CameraComponent`), so `zoomScale` is `1` at
    startup zoom (icons at `IconSize`) with **no latched reference** — the old first-valid-frame
    pixels-per-world-unit capture (and the `_hasReference` / `_referencePixelsPerWorldUnit` latch) is **gone**.
  - **`focusDepth / hexDepth`** — the **per-hex perspective factor**. `focusDepth` is the view-space depth of
    the screen-center point on the ground plane (`y = 0`), computed per frame in `ComputeFocusDepth`; `hexDepth`
    is `screenPoint.z` already computed when projecting the hex. The factor is `1` at the focus, `>1` nearer the
    camera (bottom of screen → **bigger**) and `<1` farther (distance → **smaller**) — the reference-screenshot
    look. The focus depth that the old empirical baseline also carried **cancels out** of `zoomScale *
    perspective`, which is exactly why the baseline reduces to the pure FOV ratio.
  - **Degenerate focus ray** (parallel to the ground / ground behind the camera) → `ComputeFocusDepth` returns
    `0`, and the `focusDepth > 0` guard yields perspective factor `1`. `zoomScale` still computes (FOV only), so
    **no sticky last-good state is needed** — this is what let the whole zoom calibration leave the instance.
  - This is the chosen **"option a"** (per-hex perspective), replacing the earlier uniform "option b".
  - Scale uses the default **center transform-origin**, so it composes with the `-50% -50%` centering translate
    **without shifting the anchor** — icons stay pinned to the hex center, no sliding. The focus uses a flat
    `y = 0` plane (a zoom proxy, not exact placement — placement still uses the real on-mesh center), which
    assumes terrain near `y = 0`. There is **no size clamp** — far icons may become small by design.
- **Parallel-table coexistence.** Container entities are a third Approach-B table carrying `HexIdComponent`
  (alongside `HexResource` and `ResourceView`). A bare `With<HexIdComponent>` consumer in Gameplay
  (`CameraMovementSystem` bounds) is a bounding-box computation where duplicate coords are idempotent, so
  the extra rows are harmless — but a future bare consumer that assumes one row per coord would need an
  explicit exclusion. (The forest spawners scope their hex set with `With<HexTag>`, so they already exclude
  these rows.)

## Current State
`HexResourceIconConfig` (asset) maps each `ResourceType` (from `HexResources`) to a `Sprite` via a list
of `[Serializable] struct ResourceIconEntry`. It is loaded by `HexIconsConfigLoaderSystem` (second
sequential load) into the `HexResourceIconConfigComponent` world component. Loader validation is minimal:
throws if `Entries` is null/empty (per-entry sprite + duplicate/completeness checks are a later step).
There is **no** Sprite-by-`ResourceType` lookup API — `HexIconsVisibilitySystem` scans `Entries` linearly.
The real consumer is now **`HexIconsVisibilitySystem`** (event-driven, see the Visibility invariants): on a
`HexIconsVisibilityChangedEvent` it rebuilds every hex's container from that hex's actual resources. The old
hard-coded spawn-time demo (`AddDemoIcons` / `FindResourceSprite` / `TryGetContainer`) is **gone**. Icon size
in pixels comes from `HexIconsConfig.IconSize` (default 64); `HexIconsConfig.WorldYOffset` (default 1.5) is the
world-space `+Y` lift applied before projection so icons float above the hex (consumed by
`HexIconsContainerPositionSystem`).

**The module switched from a world-space panel to a Screen-Space overlay.** `HexIconsSpawnSystem` was
rewritten: no `WorldDocumentRaycaster`, no instance transform, no root/map pixel sizing — it instantiates
the screen-space prefab, makes the overlay raycast-transparent, and **eagerly creates one empty container
entity per hex** (icons are filled in later by `HexIconsVisibilitySystem`). It still runs at priority 700
in the `MapGenerationStep` pipeline (no longer last — `MainUISpawnSystem` runs at 800). The
`HexIconsConfig.WorldHeight` / `HexIconsConfig.PixelsPerUnit` fields are now **unused** (legacy from the
world-space approach). Container positioning is now a **per-frame world→screen projection** in
`HexIconsContainerPositionSystem` (Gameplay state), so containers track the camera as it moves — the
spawn-time one-shot projection (which drifted) is gone. The screen-space switch is **experimental / under
evaluation**.
UXML/USS exist: `Prefabs/HexIconsView.uxml` holds a single empty root (`hex-icons-root`);
`Prefabs/HexIconsView.uss` only paints a transparent background. Per-hex containers and icons are created
in code at runtime (see Spawn invariants), not authored in UXML/USS.
`HexIconsView` exposes the `Root` (`hex-icons-root`) content element; the getter throws if the
UIDocument / element is missing.

## Design Decisions
- Config holds a single prefab reference. The prefab root has a Screen-Space UIDocument + HexIconsView.
- Panel topology: **one shared screen-space overlay** for the whole map. Per-hex icons live as child
  containers inside `hex-icons-root`, not as separate UIDocuments.
- **Container store is the ECS world, not a dictionary.** Each container is a parallel entity
  (`HexIdComponent` FK + `HexIconContainerComponent`), mirroring the `ResourceView` archetype (Approach B in
  the ecs-graph (`/ecs-graph`)). This lets the per-frame positioner iterate containers as an `EntitySet` and keeps the
  hex↔container join explicit via the shared `HexCoord`, with no mirror state to keep in sync.
- **Visibility is event-driven, state-truthed (variant B).** `HexIconsVisibilityComponent` (world, mutable)
  is the single source of truth; a payload-less `HexIconsVisibilityChangedEvent` only *triggers* a re-render.
  Producer writes the state then raises the event; the consumer (`HexIconsVisibilitySystem`) reads the state.
  This decouples *what the state is* from *when to re-render*, and the event's one-frame lifecycle gives the
  "react only when just-added" guarantee for free (no `WhenAdded`/diff bookkeeping). Rendering is a full
  clear-and-rebuild with **no cached state** — chosen for simplicity; the per-hex resource join is recomputed
  each event.
- `HexIconsView.uxml` references `HexIconsView.uss` via `<Style src>`, so the stylesheet is
  self-contained — wire `UIDocument.sourceAsset` to the UXML in the prefab inspector.
- `HexIconsView._document` is a serialized reference and must be wired to the prefab-root UIDocument in
  the inspector. `HexIconsView` exposes only the `Root` content element.
- `HexIconsConfig.PixelsPerUnit` and `HexIconsConfig.WorldHeight` are **legacy** from the world-space panel
  and currently unused after the screen-space switch (kept on the config for now; revisit when the
  projection step lands).
- Loader is stateless (no Box field) — ownership lives in the world component.
- `HexResourceIconConfig` is a **separate** asset, not a field on `HexIconsConfig`: icon-sprite data has a
  different authoring lifecycle (per-resource art) than the panel/prefab settings.
- `ResourceIconEntry` is a value-type `struct` (immutable, read-only accessors). `Entries` is exposed as
  `IReadOnlyList` so consumers cannot mutate the backing array.
- HexIcons references `HexResources` solely for the `ResourceType` enum.
