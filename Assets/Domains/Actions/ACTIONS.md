---
category: A
read: reference
tags:
  - actions
  - ecs
  - domain
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[GAMEPLAY_FOUNDATION](../../../GAMEPLAY_FOUNDATION.md)"
  - "[ECONOMY](../Economy/ECONOMY.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
  - "[BUILD_DISTRICT_ACTION](BuildDistrictAction/BUILD_DISTRICT_ACTION.md)"
  - "[BUILD_DISTRICT_COST](../Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[TURN_PHASES](TURN_PHASES.md)"
status: partial
code_refs:
  installers:
    - ActionsInstaller
  components:
    - ActionIdComponent
    - ActionIdAllocatorComponent
---

# Actions

The application / orchestration layer: actor verbs and cross-domain turn processing. Depends on both
`Economy` and `Actors`; nothing depends on it. This is the **domain index** — feature-areas have their
own docs (see Current State).

## Purpose
This is the top of the domain DAG **substrate → agents → verbs**: `Economy` (owner-agnostic resource /
district vocabulary) → `Actors` (identities + startup state) → `Actions`. It is the one place allowed
to know every other domain, so cross-domain scenarios live here and the lower domains stay closed to
modification (Open-Closed): a new resource type touches only Economy, a new actor only Actors, a new
verb / scenario only Actions.

Planned content (rest pending):
- Mayor / Noble verbs — build district, operate district, negotiate, invest, intervene.
- Turn-phase scenarios that span domains — e.g. the **upkeep** scenario *orchestrates* per-owner
  resource upkeep, while the resource math itself stays an owner-agnostic helper in `Economy`
  (scenario in Actions, rule in the domain).

## Design Decisions
- **Orchestration here, mechanics in the domains.** A scenario in Actions composes domain operations;
  it must not re-implement them. Keeping the rule (e.g. upkeep arithmetic) in `Economy` and only the
  sequencing in `Actions` is what preserves Open-Closed inside this layer.
- **Boot stays the init engine.** World-init still runs through Boot's `MapGenerationStep` pipeline and
  per-actor spawn stages live in `Actors`. Actions does NOT own an init orchestrator or a `GameStates`
  sub-domain — that was considered and deliberately deferred until a concrete need exists.
- **Turn engine reuse.** When turn verbs land, they plug into the existing `Turn` module engine as
  phase subsystems; Actions provides the phase content, `Turn` runs it.

## Current State

PARTIAL. The domain is organized into per-feature **sub-domains** (folder = namespace segment):
- **`BuildDistrictAction/`** → [BUILD_DISTRICT_ACTION.md](BuildDistrictAction/BUILD_DISTRICT_ACTION.md) — the
  owner-scoped build **verb**. The earlier draft→snapshot→commit implementation (plus the `ResourceSpend/`
  resource-spend mechanic it called) was judged badly-done and rolled back to a bare scaffold — it is being
  rebuilt in small ECS steps, one mechanic at a time, instead of all at once. See that doc's Current State
  for what remains.
- **Shared Actions identity** — `Components/` holds `ActionIdComponent` (PK) + `ActionIdAllocatorComponent`
  (world-singleton allocator), the generic action-identity scaffold meant for any action verb, not owned by
  `BuildDistrictAction`. Kept through the rollback as the one useful piece of that code; currently dormant —
  nothing writes or reads them yet.
- **Turn phases** → [TURN_PHASES.md](TURN_PHASES.md) — the Mayor AP-restore phase (`MayorAPRestoreSubSystem`,
  still in `Systems/`; not promoted to its own sub-domain until a second related phase lands).

**Moved out to Economy (district vocabulary, keyed by `DistrictType`):** the **cost** catalogue
(`DistrictBuildCost` → [BUILD_DISTRICT_COST.md](../Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)) and the
**outcome** catalogue (`DistrictBuildOutcome` → [BUILD_DISTRICT_OUTCOME.md](../Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md))
are owner-agnostic district vocabulary; they moved to `Economy` beside `DistrictOpenCondition`. The
`BuildDistrictAction` verb, once rebuilt, reads them (cost on commit, outcome on completion) but does not
own them.

Still scaffold: Mayor/Noble verbs, cross-domain turn scenarios (upkeep arithmetic, resolution, yield
split), and the other turn phases.
