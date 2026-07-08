---
category: A
read: reference
tags: [presentation, districts, view, ecs]
related:
  - "[PRESENTATION](../PRESENTATION.md)"
  - "[FLOW_DISTRICT_BUILD](../../../Flows/FLOW_DISTRICT_BUILD.md)"
  - "[PATTERN_TRANSACTION_ENTITY](../../../Patterns/PATTERN_TRANSACTION_ENTITY.md)"
status: partial
code_refs:
  systems:
    - DistrictViewSpawnSystem
    - DistrictViewsConfigLoaderSystem
  components:
    - DistrictViewComponent
  world_components:
    - DistrictViewsConfigComponent
  tags:
    - DistrictViewTag
  configs:
    - DistrictViewsConfig
  views:
    - DistrictView
  events:
    - DistrictBuiltEvent
  installers:
    - DistrictsInstaller
---

# Districts (Presentation)

World view for districts: spawns the district prefab for every built district.

## Purpose
Pure render layer per `FLOW_DISTRICT_BUILD.md` — this subdomain renders district FACTS, never verbs.
It owns no transaction state; it only reconciles a view entity for each committed district.

## Trigger
`DistrictViewSpawnSystem` runs on `With<DistrictBuiltEvent>` (the one-frame pulse raised by
`BuildDistrictActionSystem` in `Domains.Actions.BuildDistrictAction`). Not visible to
`roslyn-mcp` — full event flow: `FLOW_DISTRICT_BUILD.md` and the ecs-graph (`/ecs-graph`).

## Public Contract & Gotchas
- `DistrictViewSpawnSystem` ignores the pulse payload and reconciles against current world state:
  every entity carrying `BuildDistrictActionTag` + `HexIdComponent` + `DistrictTypeComponent` with
  no matching view (`HexIdComponent` FK not yet present in the `DistrictViewTag` view table) gets
  its prefab instantiated at the hex centre (read from `VertexGridComponent`). This makes it
  **idempotent** — a second pulse in the same frame, or a re-entrant reconcile, finds nothing
  missing and no-ops.
- **Fails loud, not silent-skip**, on every missing prerequisite: no `DistrictViewsConfigComponent`
  or `VertexGridComponent` world component, no centre vertex for a hex, or no configured prefab for
  a `DistrictType` — each throws `InvalidOperationException` rather than skipping the district.
- `DistrictViewsConfigLoaderSystem` (Config Loader, `ConfigLoadStep`, one-shot) loads
  `DistrictViewsConfig` from Addressables, validates every entry has a non-null `Prefab`, and
  publishes `DistrictViewsConfigComponent`. It retains the Addressable `Box` for the config's
  lifetime and releases it on dispose — `DistrictViewSpawnSystem` reads the same catalogue for the
  rest of play.

## Current State
PARTIAL — reconciles off the promoted VERB entity (`BuildDistrictActionTag`), not a built-district
FACT. `FLOW_DISTRICT_BUILD.md` gap 2 targets moving the source of truth to a fact entity in
`Domains.Economy`; this module's reconcile query changes when that lands. Everything else described
above is fully implemented: config load/validate, reactive spawn, per-hex idempotent reconcile,
fail-loud on missing config/prefab/grid data.
