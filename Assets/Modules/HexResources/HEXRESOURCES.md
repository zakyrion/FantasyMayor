# HexResources

Generates logical resource data for the map. Does not render anything.

## Trigger
`HexResourcesSystem` is a **world-init pipeline step** (priority 200), run sequentially by the
**`MapCreation` game state** — it does not listen to an event itself. The pipeline runs once when the
`MainMenu` state detects `TerrainGenerationGenerateEventComponent` and switches to `MapCreation`.
Full flow: `BOOT.md` / `ECS_REFERENCE.md`.

## Non-Obvious Invariants

- Resources live on **dedicated entities** (`HexIdComponent + HexResourcesComponent`), not as tag components on hex entities.
- `HexResourcesConfig` expects unique `ResourceType` values — duplicate entries fail validation.

## Current State

All three subsystems (Forest, Clay, Fish) are fully implemented.
Visualization belongs to `HexResourcesView`.
