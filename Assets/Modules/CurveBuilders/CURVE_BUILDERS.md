# CurveBuilders

Interface contract for building and evaluating animation curves used in terrain generation.

## Purpose
A shared abstraction so terrain code can depend on a curve contract instead of a concrete
curve type. Holds only the `ICurveBuilder` interface.

## Design Decisions
- Contract-only module. All implementations live in consuming modules (currently `TerrainView`).
  This keeps the interface free of terrain-specific dependencies.

## Current State
Stable. Interface only.
