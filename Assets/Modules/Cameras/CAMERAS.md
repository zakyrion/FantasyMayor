---
category: A
read: reference
tags: [camera, ecs]
related:
  - "[USER_INPUT](../UserInput/USER_INPUT.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
status: implemented
---

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
- **No zoom baseline is stored here.** Zoom is a **fixed-FOV dolly** (`CameraMovementSystem` moves the
  camera along its forward axis; `Camera.fieldOfView` never changes), so view systems need no FOV reference:
  hex-icon sizing is pure per-hex perspective depth, which is dolly-invariant. (The former
  `ReferenceFieldOfView` field was removed when zoom stopped being FOV-driven.)

## Current State
Single world component (`CameraComponent`, just `Camera`), no systems. Consumed by `UserInput`
(`CameraMovementSystem`, `HexSelectionSystem`) and `HexIcons` (`HexIconsContainerPositionSystem`) — all read
`Camera` for projection only.
