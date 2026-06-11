# Cameras

Owns the shared scene-camera reference as world state, decoupled from any consumer module.

## Purpose
Any module that needs the active scene camera reads it here instead of depending on `UserInput`.
Named in the plural because more camera slots may be added later.

## Non-Obvious Invariants
- `CameraComponent` is a **world component**, not an entity component. `WorldInstaller` sets it once via
  `world.Set(...)`; consumers read it via `world.Get<CameraComponent>()` guarded by `world.Has<...>()`.
  It never appears in `world.GetEntities()` and cannot be matched by `With<CameraComponent>()` /
  `WhenAdded` / `WhenChanged` — see ARCHITECTURE.md "State Storage Taxonomy".
- `Camera` can be null if the serialized reference on `WorldInstaller` is unassigned — consumers
  null-check before use.
- `ReferenceFieldOfView` is the camera's **authored startup FOV**, snapshotted by `WorldInstaller` at the
  same `world.Set` — **before** any zoom input mutates `Camera.fieldOfView`. It is the neutral "1× zoom"
  baseline: a view system reads it instead of latching its own first-frame reference, and expresses a zoom
  factor as `tan(ReferenceFieldOfView/2) / tan(Camera.fieldOfView/2)`. Valid because zoom is FOV-driven
  (`CameraMovementSystem` clamps `fieldOfView`), not dolly-driven. It is **not** re-derived later — it is
  whatever `fieldOfView` was at the `world.Set`.

## Current State
Single world component (`CameraComponent`), no systems. Consumed by `UserInput`
(`CameraMovementSystem`, `HexSelectionSystem`) and `HexIcons` — `HexIconsContainerPositionSystem` reads both
`Camera` (projection) and `ReferenceFieldOfView` (per-frame zoom baseline).
