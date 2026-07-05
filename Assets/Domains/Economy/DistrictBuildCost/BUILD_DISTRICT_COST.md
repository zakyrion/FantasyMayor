---
category: A
read: reference
tags:
  - economy
  - config
  - district
  - ecs
related:
  - "[ECONOMY](../ECONOMY.md)"
  - "[DISTRICT_OPEN_CONDITION](../DistrictOpenCondition/DISTRICT_OPEN_CONDITION\
    .md)"
  - "[BUILD_DISTRICT_OUTCOME](../DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[DISTRICT_BUILD](../../../Presentation/UI/DistrictBuild/DISTRICT_BUILD.md)"
  - "[PATTERN_CONFIG](../../../../Patterns/PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](../../../../Patterns/PATTERN_CONFIG_LOADER.md)"
status: partial
code_refs: >-
  {
    "world_components": ["DistrictBuildCostsConfigComponent", "DistrictsBuildConfigComponent", "DistrictBuildCostResourcePriceComponent"],
    "systems": ["DistrictBuildCostsConfigLoaderSystem"],
    "configs": ["DistrictBuildCostConfig", "DistrictBuildCostsConfig", "DistrictsBuildConfig"],
    "types": ["ResourceCost"]
  }
---

# District Build Cost

The district-build **cost** catalogue: the per-district AP + resource price for building a district type.
Owner-agnostic district vocabulary keyed by `DistrictType`, owned by `Economy`.

## Purpose
Cost is owner-agnostic district **vocabulary** — "what does building this district type cost?" — so it lives
in `Economy` alongside the other district catalogues, not in the owner-scoped `Actions` verb layer. It is the
cost sibling of `DistrictOpenCondition` (unlock rules), joined by `DistrictType`: parallel district catalogues,
each keyed on `DistrictType`, none knowing about an owner. The `Actions` build verb (`BuildDistrictAction`)
READS this catalogue when an owner commits a build; it does not own it. One concern, one home — adding a price
never touches Actions, and the verb never owns the price table.

> **Ownership note (corrected).** This catalogue previously sat in `Actions` on the (now reversed) framing that
> "price is verb-layer state, so it lives in Actions not Economy". It is not — cost is district vocabulary that
> happened to live in Actions. It has moved to `Economy` to join `DistrictOpenCondition` as a district-keyed
> catalogue; the Actions verb reads it. The old framing must be read as history, not current design.

## Public Contract & Gotchas
- **Two catalogues, joined by `DistrictType`.** The Economy placement/gating catalogue
  (`DistrictsBuildConfigComponent`) and this cost catalogue (`DistrictBuildCostsConfigComponent`) are parallel
  lists keyed on `DistrictType`. A consumer reads placement from one and cost from the other and joins per
  district — there is no single merged config. `DistrictType` is the join key; an entry in one catalogue with
  no match in the other is a config authoring error (the build UI fails loud on it).
- **The component carries a live SO reference, not a flattened copy.** `DistrictBuildCostsConfigComponent`
  wraps the catalogue SO; the loader (`DistrictBuildCostsConfigLoaderSystem`) **retains** the addressable
  `Box` for the catalogue's lifetime and releases it in `OnDispose` (the build window reads it throughout
  play). Same shape as Economy's `DistrictsBuildConfigLoaderSystem`. Config flow rules:
  `Patterns/PATTERN_CONFIG.md` + `Patterns/PATTERN_CONFIG_LOADER.md`.
- **Loaded once at `ConfigLoadStep`** (registration: `dig.py installer EconomyInstaller`).
  It fails loud if the catalogue is missing or has null entries — until the asset is authored, the game stops
  at boot rather than running cost-blind.

- **The price value type is `ResourceCost`, not `ResourceAmount`.** `DistrictBuildCostConfig._districtPrices`
  and `DistrictBuildCostResourcePriceComponent`'s price fields/indexer hold `ResourceCost`
  (`Domains.Economy.DistrictBuildCost.Data`) — a distinct value-alias struct for the build-price role,
  structurally identical to `ResourceAmount` but never interchangeable with it. The build-commit boundary
  (`BuildDistrictActionCommitSystem`) maps each `ResourceCost` to a `ResourceAmount` by **explicit field
  assignment** before handing prices to the spend pipeline — there is no conversion operator or helper.

## Non-Obvious Invariants
- **The class now lives in Economy but still loads from the legacy addressable key `"ActionsDistrictsBuildConfig"`.**
  The authored `.asset` binds to that address; the type moved to Economy and renamed district-first, the address
  did not. Do not change the key string in code — align it Unity-side later.

## Current State
PARTIAL. The cost catalogue is loaded and **read by the district-build UI** (`Presentation.UI`) for the AP
pill + cost rows, joined to the Economy placement catalogue by `DistrictType`. No `Actions` **verb** spends it
yet — the build action itself (deduct AP / resources, place the district) is not implemented (see
`BUILD_DISTRICT_ACTION.md`). The authored SO assets (the catalogue + per-district entries) are owned outside
code.

The per-district cost entry is `DistrictBuildCostConfig` (`ApPrice`, `DistrictPrices`, `DistrictType`,
`TurnsToBuild`); the catalogue container is `DistrictBuildCostsConfig`.
