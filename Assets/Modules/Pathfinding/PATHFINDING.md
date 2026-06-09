# Pathfinding

Hex-grid BFS pathfinding over ECS entities using native Unity collections.

## How To Use Correctly
- `TryFindPath` returns `true` and fills an out `NativeList<HexCoord>` when a path exists.
- **The caller owns disposal.** Every native container the utility produces uses the
  `Allocator` you pass in — you must dispose the result list yourself.
- The search domain is built from `hexSet` entities that carry `HexIdComponent`.
  A hex not in that set is treated as impassable / non-existent.

## Design Decisions
- `HexPathfindingUtility` is stateless and registered as a singleton. Safe to reuse across calls;
  it holds no per-search state between invocations.

## Current State
Stable. Plain BFS (no weighting / diagonal cost).
