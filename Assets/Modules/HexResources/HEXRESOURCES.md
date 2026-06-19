---
category: A
read: reference
tags: [hex, resources, ecs, generation]
related:
  - "[HEXRESOURCESVIEW](../HexResourcesView/HEXRESOURCESVIEW.md)"
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
status: implemented
---

# HexResources

Generates logical resource data for the map. Does not render anything.

## Trigger
`HexResourcesSystem` is a **Pipeline Orchestrator** (world-init stage, priority 200), run
sequentially by the **`MapCreation` game state** — it does not listen to an event itself. It fans
out into the generation **Pipeline SubSystems** (Forest/Clay/Fish) in their priority order. The
pipeline runs once when the `MainMenu` state detects `TerrainGenerationGenerateEventComponent` and
switches to `MapCreation`. Roles: `ARCHITECTURE.md` "System Taxonomy". Full flow: `BOOT.md` /
`ECS_REFERENCE.md`.

## Non-Obvious Invariants

- Resources live on **dedicated entities** (`HexIdComponent + HexResourcesComponent`), not as tag components on hex entities.
- `HexResourcesConfig` expects unique `ResourceType` values — duplicate entries fail validation.

## Current State

All three subsystems (Forest, Clay, Fish) are fully implemented.
Visualization belongs to `HexResourcesView`.
