---
category: A
read: reference
tags: [input, camera, hex, ecs]
related:
  - "[CAMERAS](../Cameras/CAMERAS.md)"
  - "[TERRAIN_VIEW](../TerrainView/TERRAIN_VIEW.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
status: implemented
code_refs:
  systems:    [HexSelectionSystem, CameraMovementSystem]
  components: [PlayerInputComponent, HexSelectedComponent, CameraMovementConfigComponent, TerrainViewConfigComponent]
  events:     [SelectedHexChangedEvent]
---

# UserInput

Bridges Unity InputSystem to ECS: camera pan/drag/zoom and hex selection.

## Non-Obvious Invariants
- Both systems are **Per-frame Systems** (`ARCHITECTURE.md` "System Taxonomy"): `HexSelectionSystem`
  runs in Update, `CameraMovementSystem` in **LateUpdate** (priority 0 — before the icon projection).
  Each is anchored on the single `PlayerInputComponent` entity as its per-frame tick anchor.
- `HexSelectedComponent` is a singleton and **its absence means "nothing selected"** — systems
  must handle the no-entity case, not a null/sentinel value. Cross-module consumers:
  `HexSelectionViewSystem` (TerrainView, highlight — per-frame poll) and the MainUI reactors
  `HexInfoPanelSystem` + `ContextTabsAvailabilitySystem` (driven by the pulse below).
- `HexSelectionSystem` raises a payload-less **`SelectedHexChangedEvent`** (module `TerrainView`, beside
  `HexSelectedComponent`) on **every** selection mutation — create, deselect (dispose), re-select to another
  coord. It is the canonical "selection changed" pulse; reactive consumers reconcile against the current
  `HexSelectedComponent` instead of polling. Full flow: the ecs-graph (`/ecs-graph`).
- Selection is a **toggle**: clicking the already-selected hex removes the selection entity (and pulses).
- `HexSelectionSystem` blocks selection when the pointer is over UI (checks `EventSystem.RaycastAll`).
  Clicks consumed by UI must not select a hex.
- Hex picking intersects the pointer ray with the `y = 0` plane, then `WorldToAxial`. There is no
  per-hex collider — selection is pure math against the ground plane.
- **Zoom is a fixed-FOV dolly, not a FOV change.** `ApplyDolly` translates the camera **along its own
  `transform.forward`**, so the tilt (pitch) and `Camera.fieldOfView` stay constant and only the camera
  **height (world Y)** changes. Scroll ticks move a target height (clamped to `MinHeight`/`MaxHeight`); the
  forward translation each frame is solved from the desired height delta (`Δ / forward.y`) so the camera
  stays on its view line and the framed point does not jump. A near-horizontal camera (|`forward.y`| ≈ 0)
  cannot dolly-zoom and is skipped. Zoom clamp is by **height**, independent of the horizontal
  `ClampCameraPosition` (center-ray XZ bounds).
- `CameraMovementConfigComponent` has a static `Default` fallback. If the config fails to load,
  the camera still works with safe defaults instead of breaking.
- **Cross-module coupling:** `HexSelectionSystem` and `CameraMovementSystem` read
  `TerrainViewConfigComponent` (owned by `TerrainView`) only for `CellSize`. Changing that field
  affects input math.
- **Camera source:** the scene camera is the world component `CameraComponent`, owned by the `Cameras`
  module, read via `world.Get<CameraComponent>()` (guard with `world.Has`). It is NOT an entity.
  `CameraMovementSystem` and `HexSelectionSystem` do not iterate the camera — they read the world
  component.
- Config address is `nameof(CameraMovementConfig)` — the addressable key must match the type name.

## Input Actions Used
From `FantasyMayor.inputactions` (these names are not visible to roslyn-mcp — they live in a Unity asset):
- `UI/Click`, `UI/Point` — selection click + pointer position
- `UI/ScrollWheel` — zoom
- `UI/RightClick` — drag pan
- `Player/Move` — WASD pan
- `Player/Look` — look axis

## Current State
Stable. Camera movement and hex selection both fully working.
