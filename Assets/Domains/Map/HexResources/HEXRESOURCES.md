---
category: A
read: reference
tags: [hex, resources, ecs, generation]
related:
  - "[HEXRESOURCESVIEW](../../../Presentation/HexResources/HEXRESOURCESVIEW.md)"
status: implemented
code_refs:
  systems:    [HexResourcesSystem]
  components: [HexIdComponent, HexResourceComponent]
  tags:       [HexResourceTag]
  configs:    [HexResourcesConfig]
---

# HexResources

Generates logical resource data for the map. Does not render anything.

## Trigger

A pipeline stage run by the **`MapCreation`** state — it does **not** listen to an event itself; the
state runs it (and its generation sub-systems) sequentially. The pipeline fires once, when `MainMenu`
switches to `MapCreation` on `TerrainGenerationGenerateEventComponent`. Role, priority and the
sub-system order: `mcp__ecs-graph__system_contract HexResourcesSystem` (or `/ecs-graph`).

## Non-Obvious Invariants

- Resources live on **dedicated entities** (`HexIdComponent + HexResourceComponent + HexResourceTag`),
  not as tag components on hex entities.
- `HexResourcesConfig` expects unique `ResourceType` values — duplicate entries fail validation.

## Current State

All three generation sub-systems (Forest, Clay, Fish) are implemented. Visualization belongs to
`HexResourcesView`.
