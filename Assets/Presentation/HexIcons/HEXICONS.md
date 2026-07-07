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
`HexIconsVisibilitySystem` runs on a `HexIconsVisibilityChangedEvent` pulse (Gameplay — base set = the
event; the truth lives in the `HexIconsVisibilityComponent` world component). Producer today:
`GameplayState.EnterAsync`; later a UI toggle. Not visible in roslyn-mcp — full event flow + system
roles/priorities: the ecs-graph (`/ecs-graph`).

## Non-Obvious Invariants
- Both `HexIconsConfigComponent` and `HexResourceIconConfigComponent` own their `Box` — the loader
  transfers ownership on `Set` and nulls the local. This **deviates** from the standard
  ADDRESSABLE_PATTERNS.md rule ("loader retains Box"); acceptable since a singleton config lives for the
  app lifetime, with no explicit disposal path at world teardown.
- **`HexResourceIconConfig`/`HexResourceIconConfigComponent` are a read-only cross-module contract.**
  `HexInfoPanelResourcesSystem` (HexesUI) reads the same world component to render its resource chips —
  changing their shape affects that module too.
- One loader (`HexIconsConfigLoaderSystem`) loads **both** configs inside a single try/finally: both
  boxes stay owned until every `World.Set` has run. A failure/cancel on the second load rolls back the
  first via the shared `finally` (`DisposeBox`) — no half-committed state, no leak.

## Non-Obvious Invariants (Spawn)
- **Screen-space overlay.** The prefab is a Screen-Space UI Toolkit document (configured prefab-side).
  Spawn does **not** touch a `WorldDocumentRaycaster`, the instance `transform`, or root pixel size — the
  panel covers the screen via its own `PanelSettings`. It instantiates the prefab, makes the overlay
  raycast-transparent, and **creates one empty container entity per hex**. It does **not** add any icons —
  that is the event-driven `HexIconsVisibilitySystem`'s job (Gameplay). Spawn no longer reads the camera,
  the `VertexGrid`, or `HexResourceIconConfigComponent`.

```clojure
(cond
  (prefab-has-no-HexIconsView?)                 :throw+destroy-the-instance
  (HexIconsConfigComponent-or-prefab-missing?)  :throw)
```

- `HexIconsViewComponent` is a world component (not an entity), read via `world.Get`.
- **Raycast transparency, applied to every element in the overlay, not just the root.** UI Toolkit picking
  does **not** propagate to children, so each element is marked individually via `EnablePicking(false)`
  (Unity.AppUI): `ApplyRaycastTransparent()` sweeps `raycast-transparent`-classed UXML elements at spawn
  (the USS class alone does nothing); `MakeRaycastTransparent` marks per-hex containers and each icon
  individually since both are created *after* that sweep. Without this the overlay would swallow every
  pointer ray and block world/hex selection. Requires the `Unity.AppUI` assembly reference.
- **Per-hex container entity.** Spawn **eagerly** creates one container `VisualElement` per hex selected
  via `With<HexTag>` (bare `HexIdComponent` would also match the resource / resource-view parallel
  tables) — no dictionary, the container store is the ECS entity set. Each gets a parallel container
  entity carrying `HexIdComponent` (FK) + `HexIconContainerComponent`; the element self-centers via
  `translate: -50% -50%`. Spawn does **not** position it — that is the position system's job.
## Non-Obvious Invariants (Visibility / icon rendering)
- **State (variant B): `HexIconsVisibilityComponent`** (world, mutable `bool IsVisible`) is the single
  source of truth for whether icons are shown. The *producer writes the state, then raises the event*;
  the consumer only reads the state. **Producer (for now):** `GameplayState.EnterAsync` sets
  `IsVisible = true` and raises one event on entering Gameplay; later a UI control does the same on toggle.
- **Event: `HexIconsVisibilityChangedEvent`** is a one-frame, payload-less signal ("re-render now"),
  disposed by `EventCleanupSystem` at end of tick (mirrors the `HexesUI` producer pattern).
- **Consumer: `HexIconsVisibilitySystem`.** Its base set **is the event set**, so `Update` only fires on a
  frame an event exists (zero idle cost). On an event it resolves prerequisites fail-loud, then iterates
  **all** container entities: `container.Clear()`, and when `IsVisible` rebuilds each from that hex's
  resources by scanning the resource table (matched by `Coords`) and doing a linear `TryFindSprite` over
  `HexResourceIconConfig.Entries`. **No render state is cached** — every event is a full clear-and-rebuild.

```clojure
(def TryFindSprite
  {:entry-missing     :skip    ;; not an error — not every resource has art
   :entry-sprite-null :skip})
```

## Non-Obvious Invariants (Per-frame positioning)
- **`HexIconsContainerPositionSystem` re-projects every container each `LateUpdate`**, after
  `CameraMovementSystem` (replacing the old one-shot spawn projection). Running it in `Update` instead
  lags the camera by one frame — positions computed before the camera moves that frame make icons slide
  visibly while panning/zooming. `ecsg.py explain HexIconsContainerPositionSystem` gives role/priority.
- **Per-frame resolve in `PreUpdate`, per-entity project in `Update`.** `PreUpdate` resolves the panel,
  camera, `VertexGrid`, and `HexIconsConfigComponent` once per frame, computes zoom scale + focus depth,
  and packs it into one frame-stamped `FrameBox<FramePose>` (`Core`) — **throws fail-loud if any is
  missing** (by Gameplay, spawn has already run, so absence is a real error). The system holds **no other
  cross-frame state**: the `FrameBox` (`[StateAllowed]`) is valid for that frame only — a stale `.Value` in
  a later frame **throws** instead of reading outdated data. There is no latched zoom reference (see scale
  below).
- **Projection convention.** Per entity: `HexIdComponent.Coords` → the hex's **real on-mesh center** from
  the `VertexGrid` (same geometry the selection outline uses, so X/Z *and* height match what renders — a
  flat `y = 0` point caused a diagonal offset under the tilted camera). The center is **lifted by
  `HexIconsConfig.WorldYOffset`** (world-space `+Y`, default `1.5`, read live per frame) so the icon floats
  above the hex and foreshortens with perspective; the lifted point's depth feeds both position and scale.
  `Camera.WorldToScreenPoint` → flip Y (`Screen.height − y`, `RuntimePanelUtils.ScreenToPanel` does not
  flip itself) → a hex missing from the grid, or `z ≤ 0` (behind camera), → container `display: None`.
  Runs unconditionally every `LateUpdate` (no camera-moved gate yet).
- **Per-hex perspective scale** (the chosen design, replacing an earlier uniform scale). Final
  `style.scale` = `zoomScale * (focusDepth / hexDepth)`:
  - `zoomScale` is the map-wide baseline from FOV alone: `tan(ReferenceFieldOfView/2) /
    tan(Camera.fieldOfView/2)` (zoom is FOV-driven, not distance-driven). `ReferenceFieldOfView` is the
    camera's startup FOV, so `zoomScale` is `1` at startup — **no latched reference**, unlike the old
    first-valid-frame pixels-per-world-unit capture it replaced.
  - `focusDepth / hexDepth` is the per-hex perspective factor (`focusDepth` = view-space depth of the
    screen-center point on the ground plane; `hexDepth` = the hex's own projected depth): `1` at the focus,
    `>1` nearer the camera (bigger), `<1` farther (smaller).
  - **Degenerate focus ray** (parallel to / behind the ground) → factor `1`; `zoomScale` still computes
    from FOV alone — no sticky last-good state needed. Composes with the centering translate without
    shifting the anchor. **No size clamp** — far icons may become small by design.
- **Parallel-table coexistence.** Container entities are a third Approach-B table carrying
  `HexIdComponent` (alongside `HexResource`/`ResourceView`) — a bare `With<HexIdComponent>` consumer (e.g.
  `CameraMovementSystem` bounds) tolerates the duplicate rows as idempotent; a future consumer assuming
  one row per coord needs an explicit `With<HexTag>` exclusion.

## Current State
`HexResourceIconConfig` (asset) maps each `ResourceType` to a `Sprite` via `[Serializable] struct
ResourceIconEntry`, loaded by `HexIconsConfigLoaderSystem` (second sequential load) into
`HexResourceIconConfigComponent`. Loader validation is minimal: throws if `Entries` is null/empty
(per-entry sprite + duplicate/completeness checks are a later step). There is **no**
Sprite-by-`ResourceType` lookup API — `HexIconsVisibilitySystem` scans `Entries` linearly.

**The module switched from a world-space panel to a Screen-Space overlay** (`HexIconsConfig.WorldHeight` /
`.PixelsPerUnit` are now **unused legacy**). Container positioning is now a per-frame world→screen
projection in `HexIconsContainerPositionSystem` (Gameplay), replacing the old drifting one-shot spawn-time
projection. The screen-space switch is **experimental / under evaluation**. `Prefabs/HexIconsView.uxml`/
`.uss` hold only the empty `hex-icons-root` + a transparent background — containers/icons are created in
code (see Spawn invariants), not authored in UXML/USS.

## Design Decisions
- Panel topology: **one shared screen-space overlay** for the whole map. Per-hex icons live as child
  containers inside `hex-icons-root`, not as separate UIDocuments.
- **Container store is the ECS world, not a dictionary** (see Spawn invariants) — mirrors the
  `ResourceView` archetype (Approach B, ecs-graph), lets the positioner iterate as an `EntitySet`, and
  keeps the hex↔container join explicit via `HexCoord` with no mirror state.
- **Visibility is event-driven, state-truthed (variant B)** (see Visibility invariants) — decouples *what
  the state is* from *when to re-render*, and the event's one-frame lifecycle gives the "react only when
  just-added" guarantee for free (no `WhenAdded`/diff bookkeeping). Full clear-and-rebuild with no cached
  state was chosen for simplicity.
- `HexResourceIconConfig` is a **separate** asset, not a field on `HexIconsConfig`: icon-sprite data has a
  different authoring lifecycle (per-resource art) than the panel/prefab settings. Loader is stateless (no
  Box field) — ownership lives in the world component. HexIcons references `HexResources` solely for the
  `ResourceType` enum.
