---
category: A
read: reference
tags: [economy, district, config, ecs]
related:
  - "[ECONOMY](../ECONOMY.md)"
  - "[DISTRICT_OPEN_CONDITION](../DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
  - "[BUILD_DISTRICT_COST](../DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[PATTERN_POLYMORPHIC_CATALOGUE](../../../../Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md)"
  - "[PATTERN_CONFIG](../../../../Patterns/PATTERN_CONFIG.md)"
  - "[PATTERN_CONFIG_LOADER](../../../../Patterns/PATTERN_CONFIG_LOADER.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](../../../../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
status: partial
code_refs:
  configs:          [DistrictBuildOutcomesConfig, DistrictBuildOutcomeConfig, SpawnCityCenterOutcomeConfig]
  world_components: [DistrictBuildOutcomesConfigComponent]
  components:       [DistrictTypeComponent]
  tags:             [DistrictBuildOutcomeTag, SpawnCityCenterOutcomeTag]
  systems:          [DistrictBuildOutcomesConfigLoaderSystem, DistrictBuildOutcomeSpawnSystem, DistrictBuildOutcomeSpawnSubSystem, SpawnCityCenterOutcomeSubSystem]
---

# District Build Outcome

The district-build **outcome** catalogue: the per-district consequence that runs when a district finishes
building (first kind: spawn the City Center). Owner-agnostic district vocabulary keyed by `DistrictType`,
authored as a polymorphic catalogue and materialized into one entity per outcome.

## Purpose
The build result differs per district type, so each outcome is its own `ScriptableObject` subclass of the
abstract `DistrictBuildOutcomeConfig`. A single container SO lists them; a spawn pipeline turns each authored
entry into its own entity so a future apply step can join outcomes per district type like any other table.
It is the outcome sibling of `DistrictOpenCondition` (unlock rules) and `DistrictBuildCost` (price): district
vocabulary keyed on `DistrictType`, none knowing about an owner. The `Actions` build verb reads/joins it on
completion; it does not own it.

## Relationship to DistrictOpenCondition (the sibling it mirrors)
This catalogue is a **`DistrictOpenCondition` clone — the spawn/routing half ONLY**. It reuses that sub-domain's
config-side and spawn-family shape verbatim (`PATTERN_POLYMORPHIC_CATALOGUE.md`), but has **no evaluator family**:
outcomes are a static lookup table, not a per-turn re-evaluated gate. Where `DistrictOpenCondition` adds a
non-routing evaluator loop that Sets/Removes `DistrictCanBeBuildTag` each turn, this sub-domain stops after the
spawn — the join-and-apply happens once, at action completion (FUTURE, see Current State).

## Public Contract & Gotchas
- **A outcome is an ENTITY, not a world component — same deliberate departure as `DistrictOpenCondition`.** Only
  the CONTAINER (`DistrictBuildOutcomesConfig`) is a world component (`DistrictBuildOutcomesConfigComponent`,
  carrying the live SO reference). `DistrictBuildOutcomeSpawnSystem` reads it and creates one entity per concrete
  outcome. The "config = world component, never an entity" rule of `Patterns/PATTERN_CONFIG.md` applies to the
  container only.
- **Outcome entity table (archetype):** key `DistrictTypeComponent` (FK = the district this outcome belongs to)
  + discriminator `DistrictBuildOutcomeTag` (= "this row is a build-outcome") + a **per-kind marker**
  (`SpawnCityCenterOutcomeTag` for the first kind). Spawn-only: there is **no evaluator component/tag**. Query by
  the table (key + discriminator), never bare `With<DistrictTypeComponent>` (Table Rule).
- **The loader retains the addressable `Box`** for the catalogue's lifetime (the orchestrator reads the concrete
  SO references at `MapGenerationStep`, after `ConfigLoadStep`); released in `OnDispose`. Same shape as
  `DistrictOpenConditionsConfigLoaderSystem`.
- **Adding an outcome kind = one `Spawn<District>OutcomeConfig` subclass + its own subsystem (Open-Closed).**
  Each district gets its OWN outcome subclass (unique consequence config) and its OWN
  `DistrictBuildOutcomeSpawnSubSystem` registered as the base type. The orchestrator routes each authored config
  to the subsystem whose concrete type matches (`TrySpawn`), and **fails loud** on a null entry or a config type
  no subsystem handles. First concrete kind: `SpawnCityCenterOutcomeConfig` — parameter-less (the `DistrictType`
  key already says which district to spawn), so its per-kind marker is the empty tag `SpawnCityCenterOutcomeTag`.

## Non-Obvious Invariants
- SO configs are **pure data** — the type-switch lives in the spawn subsystems, never as polymorphic behavior on
  the `ScriptableObject`.
- The orchestrator (`DistrictBuildOutcomeSpawnSystem`) runs **per map** at `MapGenerationStep`, **priority 930**
  (in the domain-spawn cluster, after `DistrictOpenCondition` spawn 920 / bootstrap 925); the container is loaded
  **once** at `ConfigLoadStep`. Re-entering map creation re-spawns the outcome entities into the fresh world.
- Addressable key is `"DistrictBuildOutcomesConfig"` — distinct from the cost catalogue's legacy key.

## Current State
PARTIAL — **spawn half only**. Container config + abstract base + first concrete kind
(`SpawnCityCenterOutcomeConfig` / `SpawnCityCenterOutcomeSubSystem`, spawns the City Center) + loader + spawn
orchestrator (priority 930) + per-kind subsystem are implemented and wired in `EconomyInstaller`. The catalogue
materializes into the outcome entity table.

**Apply/join at action-completion is FUTURE — not built.** The completed `BuildDistrictAction`'s `DistrictType`
joining into this table and running the matching outcome is the Actions build-verb slice (see
`BUILD_DISTRICT_ACTION.md`); nothing consumes the outcome entities yet. Authored `.asset` (the container +
per-outcome assets) and the addressable key `DistrictBuildOutcomesConfig` are owned outside code.
