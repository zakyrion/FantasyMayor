# TerrainGenerator

Procedural terrain generation: hex grid creation, mountains with foothills, and water (river / lake / sea).

## Trigger
`TerrainGenerationSystem` is the first **Pipeline Stage** (priority 100) — strictly, a **Pipeline
Orchestrator**: it creates the hex grid itself, then fans out into the generation **Pipeline
SubSystems** (mountain/river/lake/sea). It is run sequentially by the **`MapCreation` game state**
(`Boot.Implementation`). The pipeline runs once when the `MainMenu` state detects
`TerrainGenerationGenerateEventComponent` (raised by the HexesUI Generate button) and switches to
`MapCreation`, which runs every `IPrioritizedUniTaskSystem<TerrainGenerationStep>` stage in priority
order. `TerrainGenerationSystem` no longer publishes a separate resource-generation event — the
pipeline drives resource generation directly. Roles: `ARCHITECTURE.md` "System Taxonomy". Full flow:
`BOOT.md` / `ECS_REFERENCE.md`.

## Hex Level Convention
This is the cross-module contract every terrain consumer depends on. Level is the source of truth;
tags are synced from it.

| Level | Meaning | Synced tag |
|---|---|---|
| `-1` | Water | `HexWaterTag` |
| `0` | Plain | `HexPlainTag` |
| `1` | Foothill (bedhill) | `HexBedhillTag` |
| `2` | Mountain | `HexMountTag` |

## Non-Obvious Invariants
- Generation flow: `TerrainGenerationSystem` creates every hex at Level 0, runs the subsystems in
  priority order to mutate levels, then **syncs tags from the final level**. It publishes no event —
  the next pipeline stage simply runs after it. Tags are always derived, never set independently
  of level.
- `WaterType` (`None | River | Lake | Sea`) is **mutually exclusive** — exactly one water feature
  per generation, chosen in `TerrainGenerationConfigComponent`.
- All subsystems operate on the shared hex set; later subsystems see the level changes made by
  earlier ones. Order is priority-driven and significant.
- Config address is `nameof(TerrainGenerationConfig)` — addressable key must match the type name.

## Subsystems (what each produces)
- **MountainGenerationSubSystem** — multi-seed BFS mountain body + mandatory and general foothills.
- **RiverGenerationSubSystem** — a river path between two map edges (uses `IHexPathfindingUtility`).
- **LakeGenerationSubSystem** — one lake body biased toward map center.
- **SeaGenerationSubSystem** — one sea body biased toward a map edge.

(Algorithm details live in the subsystem source as comments — not duplicated here.)

## Current State
Stable. All generation paths implemented.
