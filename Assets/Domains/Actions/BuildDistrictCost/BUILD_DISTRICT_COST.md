---
category: A
read: reference
tags: [actions, config, district, ecs]
related:
  - "[ACTIONS](../ACTIONS.md)"
  - "[ECONOMY](../../Economy/ECONOMY.md)"
  - "[PATTERN_CONFIG](../../../../Patterns/PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](../../../../Patterns/PATTERN_CONFIG_LOADER.md)"
status: partial
code_refs:
  world_components: [DistrictBuildCostsConfigComponent, DistrictsBuildConfigComponent]
  systems:          [DistrictBuildCostsConfigLoaderSystem]
  configs:          [DistrictBuildCostConfig, DistrictBuildCostsConfig, DistrictsBuildConfig]
---

# Build District Cost

The district-build **cost** config flow: the Actions domain owns the per-district AP + resource price
catalogue. Buildability/placement stays in `Economy`.

## Purpose
The build verb's **price** is verb-layer state, so it lives in `Actions`, not in the owner-agnostic
`Economy` substrate. `Economy.DistrictBuildingConfig` keeps only *where* a district may be placed
(terrain + required hex resource, via `CanBuildOn`); this area owns *what it costs*. One concern, one
home — adding a price never touches Economy, and gating never touches Actions.

## Public Contract & Gotchas
- **Two catalogues, joined by `DistrictType`.** The Economy gating catalogue
  (`DistrictsBuildConfigComponent`) and this cost catalogue (`BuildDistrictCostsConfigComponent`) are
  parallel lists keyed on `DistrictType`. A consumer reads gating from one and cost from the other and
  joins per district — there is no single merged config. `DistrictType` is the join key; an entry in one
  catalogue with no match in the other is a config authoring error (the build UI fails loud on it).
- **The component carries a live SO reference, not a flattened copy.** `BuildDistrictCostsConfigComponent`
  wraps the catalogue SO; the loader (`BuildDistrictCostsConfigLoaderSystem`) **retains** the addressable
  `Box` for the catalogue's lifetime and releases it in `OnDispose` (the build window reads it throughout
  play). Same shape as Economy's `DistrictsBuildConfigLoaderSystem`. Config flow rules:
`Patterns/PATTERN_CONFIG.md` + `Patterns/PATTERN_CONFIG_LOADER.md`.
- **Loaded once at `ConfigLoadStep`** (registration: `mcp__di-graph__installer_registrations ActionsInstaller`).
  It fails loud if the catalogue is missing or has null entries — until the asset is authored, the game stops
  at boot rather than running cost-blind.

## Current State
PARTIAL. The cost catalogue is loaded and **read by the district-build UI** (`Presentation.UI`) for the
AP pill + cost rows, joined to the Economy gating catalogue by `DistrictType`. No Actions **verb** spends
it yet — the build action itself (deduct AP / resources, place the district) is not implemented. The
authored SO assets (the catalogue + per-district entries) are owned outside code.

The per-district cost entry is `BuildDistrictCostConfig`; the catalogue container is `BuildDistrictCostsConfig`.
Addressable key is still `"ActionsDistrictsBuildConfig"` (the authored `.asset` binds to it — the class renamed,
the address did not).
