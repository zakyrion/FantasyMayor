---
category: A
read: reference
tags: [terrain, curves]
related:
  - "[TERRAIN_VIEW](../../Presentation/Terrain/TERRAIN_VIEW.md)"
status: implemented
code_refs:
  types: [ICurveBuilder]
---

# CurveBuilders

Interface contract for building and evaluating animation curves used in terrain generation.

## Purpose
A shared abstraction so terrain code can depend on a curve contract instead of a concrete
curve type. Holds only the `ICurveBuilder` interface.

## Design Decisions
- Contract-only module. All implementations live in consuming modules (currently `TerrainView`).
  This keeps the interface free of terrain-specific dependencies.

- Layout deviation: flat utility-style module (no feature-folder layout) — a single-interface
  contract needs none.

## Current State
Stable. Interface only.
