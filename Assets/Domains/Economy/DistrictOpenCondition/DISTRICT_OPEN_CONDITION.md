---
category: A
read: reference
tags: [economy, district, config, ecs]
related:
  - "[ECONOMY](../ECONOMY.md)"
  - "[DISTRICT_BUILD_COST](../../Actions/DISTRICT_BUILD_COST.md)"
  - "[DISTRICT_BUILD](../../../../Presentation/UI/DistrictBuild/DISTRICT_BUILD.md)"
  - "[PATTERN_CONFIG](../../../../Patterns/PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](../../../../Patterns/PATTERN_CONFIG_LOADER.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](../../../../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
status: partial
code_refs:
  configs:          [DistrictOpenConditionsConfig, DistrictOpenConditionConfig, DistrictExistConditionConfig, DistrictSingleOpenConditionConfig]
  world_components: [DistrictOpenConditionsConfigComponent]
  components:       [DistrictTypeComponent, DistrictExistConditionComponent]
  tags:             [DistrictOpenConditionTag, DistrictSingleOpenConditionTag, DistrictCanBeBuildTag]
  systems:          [DistrictOpenConditionsConfigLoaderSystem, DistrictOpenConditionSpawnSystem, DistrictOpenConditionSpawnSubSystem, DistrictExistConditionSpawnSubSystem, DistrictSingleOpenConditionSpawnSubSystem]
---

# District Open Conditions

The district-build **unlock** rules ("how to unblock building of a district type"), authored as a polymorphic
catalogue and materialized into one entity per condition. The third district concern, beside
`DistrictBuildingConfig` (where it can be placed) and the Actions cost catalogue (what it costs).

## Purpose
Open-conditions differ widely per district type, so each is its own `ScriptableObject` subclass of the
abstract `DistrictOpenConditionConfig`. A single container SO lists them; a spawn pipeline turns each authored
entry into its own entity so a future build-gate can query conditions like any other table.

## Public Contract & Gotchas
- **A condition is an ENTITY, not a world component — deliberate departure from the config flow.** Only the
  CONTAINER (`DistrictOpenConditionsConfig`) is a world component (`DistrictOpenConditionsConfigComponent`,
  carrying the live SO reference). `DistrictOpenConditionSpawnSystem` reads it and creates one entity per
  concrete condition. The "config = world component, never an entity" rule of `Patterns/PATTERN_CONFIG.md` applies to
  the container only.
- **Condition entity table:** key `DistrictTypeComponent` (FK = the gated district type) + discriminator
  `DistrictOpenConditionTag` (= "this row is a condition") + a **per-kind discriminator**. FK is 1:N — a
  district type may carry several conditions. Query by the table (key + discriminator), never bare
  `With<DistrictTypeComponent>` (Table Rule).
- **Per-kind discriminator differs by whether the kind carries data:**
  - data-carrying kind → a payload **component** that doubles as the discriminator
    (`DistrictExistConditionComponent { RequiredDistrict }` — present ⇒ this is an exist-condition);
  - parameter-less kind → an empty **tag** (`DistrictSingleOpenConditionTag` — present ⇒ single-instance
    condition). The tag is the parameter-less analogue of the payload component; without it a parameter-less
    kind is indistinguishable from any other.
- **`DistrictCanBeBuildTag` is a separate, read-only buildability marker — NOT the condition discriminator.**
  It marks a condition entity whose gated district is currently buildable; the DistrictBuild list reads entities
  carrying it (key `DistrictTypeComponent`) to populate the picker (`DISTRICT_BUILD.md`). A future condition
  evaluator owns attaching/removing it per whether the conditions are met — nothing attaches it yet.
- **`DistrictTypeComponent` is shared district-type vocabulary**, defined in `Economy/District/Components`, not
  here — `IEquatable` so any table can key on it. This sub-area only consumes it as the condition FK. Note its
  value is the **gated** district; a kind's payload may carry a *different* `DistrictType` as a parameter
  (`DistrictExistConditionComponent.RequiredDistrict` = the district that must exist), so the two are opposite
  roles of the same enum.
- **The loader retains the addressable `Box`** for the catalogue's lifetime (the orchestrator reads the
  concrete SO references at `MapGenerationStep`, after `ConfigLoadStep`); released in `OnDispose`. Same shape as
  `DistrictsBuildConfigLoaderSystem`.
- **Adding a condition kind = three pieces:** a concrete `DistrictOpenConditionConfig` subclass, its per-kind
  discriminator (payload component if it has parameters, else a marker tag), and a
  `DistrictOpenConditionSpawnSubSystem` registered as the base type. The orchestrator routes each authored
  config to the subsystem whose concrete type matches (`TrySpawn`), and **fails loud** on a null entry or a
  config type no subsystem handles.

## Non-Obvious Invariants
- SO configs are **pure data** — the type-switch lives in the spawn subsystems, never as polymorphic behavior
  on the `ScriptableObject`.
- The orchestrator runs **per map** (`MapGenerationStep`, priority 920, in the domain-spawn cluster after
  City/Mayor); the container is loaded **once** at `ConfigLoadStep`. Re-entering map creation re-spawns the
  condition entities into the fresh world.

## Design Decisions
- **Spawn pipeline over a flattened component:** conditions are heterogeneous and queried at runtime, so they
  are entities (one table) rather than a flattened list on one world component — a future gate can react to /
  join them per district type without unpacking a config blob every check.
- **Orchestrator + per-type subsystem** (see `Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md`) instead of a single type-switch: each condition kind owns
  its own entity-construction, and the family is DI-collected — a new kind plugs in without touching the
  orchestrator.

## Current State
PARTIAL. Container config + abstract base + two concrete kinds + loader + spawn orchestrator + per-kind
subsystems are implemented and wired in `EconomyInstaller`:
- `DistrictExistCondition` — the gated district opens once a `RequiredDistrict` exists (payload component).
- `DistrictSingleOpenCondition` — the gated district is buildable only while zero instances of it exist
  (e.g. CityCenter; parameter-less marker tag).

The DistrictBuild list reads entities tagged `DistrictCanBeBuildTag` to populate the picker, but **nothing
attaches that tag yet** and **the condition-evaluation gate (Exist / Single) is not implemented** — so the tag
is defined and consumed, never produced. Authored `.asset` (the container + per-condition assets) and the
addressable key `DistrictOpenConditionsConfig` are owned outside code.
