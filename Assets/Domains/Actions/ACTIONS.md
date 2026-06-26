---
category: A
read: reference
tags: [actions, ecs, domain]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[GAMEPLAY_FOUNDATION](../../../GAMEPLAY_FOUNDATION.md)"
  - "[ECONOMY](../Economy/ECONOMY.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
status: scaffold
---

# Actions

The application / orchestration layer: actor verbs and cross-domain turn processing. Depends on both
`Economy` and `Actors`; nothing depends on it.

## Purpose
This is the top of the domain DAG **substrate → agents → verbs**: `Economy` (owner-agnostic resource /
district vocabulary) → `Actors` (identities + startup state) → `Actions`. It is the one place allowed
to know every other domain, so cross-domain scenarios live here and the lower domains stay closed to
modification (Open-Closed): a new resource type touches only Economy, a new actor only Actors, a new
verb / scenario only Actions.

Planned content (none built yet):
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
SCAFFOLD. Assembly definition only — `Domains.Actions` references `Domains.Economy` + `Domains.Actors`
and contains no code yet. Infra references (DefaultECS.Extensions, Boot.Core, VContainer, UniTask, …)
are added with the first system. No archetypes, systems, or events.
