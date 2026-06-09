# UserInput

Bridges Unity InputSystem to ECS: camera pan/drag/zoom and hex selection.

## Non-Obvious Invariants
- `SelectedHexComponent` is a singleton and **its absence means "nothing selected"** — systems
  must handle the no-entity case, not a null/sentinel value.
- Selection is a **toggle**: clicking the already-selected hex removes the selection entity.
- `HexSelectionSystem` blocks selection when the pointer is over UI (checks `EventSystem.RaycastAll`).
  Clicks consumed by UI must not select a hex.
- Hex picking intersects the pointer ray with the `y = 0` plane, then `WorldToAxial`. There is no
  per-hex collider — selection is pure math against the ground plane.
- `CameraMovementConfigComponent` has a static `Default` fallback. If the config fails to load,
  the camera still works with safe defaults instead of breaking.
- **Cross-module coupling:** `HexSelectionSystem` and `CameraMovementSystem` read
  `TerrainViewConfigComponent` (owned by `TerrainView`) only for `CellSize`. Changing that field
  affects input math.
- **Camera source:** the scene camera is the world component `CameraComponent`, owned by the `Cameras`
  module, read via `world.Get<CameraComponent>()` (guard with `world.Has`). It is NOT an entity.
  `CameraMovementSystem` and `HexSelectionSystem` are entity-set systems anchored on the single
  `PlayerInputComponent` entity (their per-frame tick anchor) — they do not iterate the camera.
- Config address is `nameof(CameraMovementConfig)` — the addressable key must match the type name.

## Input Actions Used
From `FantasyMayor.inputactions` (these names are not visible to graphify — they live in a Unity asset):
- `UI/Click`, `UI/Point` — selection click + pointer position
- `UI/ScrollWheel` — zoom
- `UI/RightClick` — drag pan
- `Player/Move` — WASD pan
- `Player/Look` — look axis

## Current State
Stable. Camera movement and hex selection both fully working.
