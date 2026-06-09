# Cameras

Owns the shared scene-camera reference as world state, decoupled from any consumer module.

## Purpose
Any module that needs the active scene camera reads it here instead of depending on `UserInput`.
Named in the plural because more camera slots may be added later.

## Non-Obvious Invariants
- `CameraComponent` is a **world component**, not an entity component. `WorldInstaller` sets it once via
  `world.Set(...)`; consumers read it via `world.Get<CameraComponent>()` guarded by `world.Has<...>()`.
  It never appears in `world.GetEntities()` and cannot be matched by `With<CameraComponent>()` /
  `WhenAdded` / `WhenChanged` — see ARCHITECTURE.md "Config Component Storage".
- `Camera` can be null if the serialized reference on `WorldInstaller` is unassigned — consumers
  null-check before use.

## Current State
Single world component (`CameraComponent`), no systems. Consumed by `UserInput`
(`CameraMovementSystem`, `HexSelectionSystem`) and `HexIcons` (world-space raycaster event camera).
