---
category: A
read: reference
tags: [terrain, generation, hex, ecs]
related:
  - "[TERRAIN_VIEW](../../../Presentation/Terrain/TERRAIN_VIEW.md)"
  - "[HEX_CORE](../Hex/HEX_CORE.md)"
status: implemented
code_refs:
  systems:    [GenerationSystem, MountainGenerationSubSystem, RiverGenerationSubSystem, LakeGenerationSubSystem, SeaGenerationSubSystem]
  components: [HexTypeComponent, HexLevelComponent, HexIdComponent, TerrainGenerationConfigComponent]
  tags:       [HexTag]
---

# TerrainGenerator

Procedural terrain generation: hex grid creation, mountains with foothills, and water (river / lake / sea).

## Trigger
`GenerationSystem` is the generation pipeline's **orchestrator** (role/priority:
`mcp__ecs-graph__system_contract GenerationSystem`): it creates the hex grid itself, then fans out
into the generation **Pipeline SubSystems** (mountain/river/lake/sea). It is run sequentially by the **`MapCreation` game state**
(`Boot.Implementation`). The pipeline runs once when the `MainMenu` state detects
`TerrainGenerationGenerateEventComponent` (raised by the HexesUI Generate button) and switches to
`MapCreation`, which runs every `IPrioritizedUniTaskSystem<MapGenerationStep>` stage in priority
order. `GenerationSystem` no longer publishes a separate resource-generation event — the
pipeline drives resource generation directly. Roles: `ARCHITECTURE.md` "System Taxonomy". Full flow:
`BOOT.md` / the ecs-graph (`/ecs-graph`).

## Hex Level Convention
This is the cross-module contract every terrain consumer depends on. Level is the source of truth;
the `HexTypeComponent` is synced from it.

| Level | Meaning | HexType |
|---|---|---|
| `-1` | Water | `Water` |
| `0` | Plain | `Plain` |
| `1` | Foothill (bedhill) | `Bedhill` |
| `2` | Mountain | `Mount` |

## Non-Obvious Invariants
- Generation flow: `GenerationSystem` creates every hex at Level 0, runs the subsystems in
  priority order to mutate levels, then **sets HexTypeComponent from the final level** (AssignHexTypes).
  It publishes no event — the next pipeline stage simply runs after it. The type is always derived from
  level, never set independently
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
