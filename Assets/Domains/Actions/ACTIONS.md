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
status: partial
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

Planned content (first turn phase landed — see Current State; the rest pending):
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
PARTIAL. First real system landed: the AP-restore turn phase.

- `MayorActionPointsRestoreSubSystem` (`Systems/`) — a `TurnPhaseSubSystem` (Upkeep band) that, at the start
  of each new turn, resets every Mayor's live `ActionPoint` resource stack to `MayorAPRestoreComponent.Value`.
  Action Points do not carry over between turns, so it is a SET (reset to full), not an accumulate. Reads the
  Mayor row (`MayorIdComponent` + `MayorAPRestoreComponent`) and re-Sets the matching `ActionPoint` resource
  stack found via `EntityMultiMap<MayorIdComponent>`. Runs off the turn thread pool but switches to the main
  thread before the world write (TURN.md invariant).
- `ActionsInstaller` registers the phase `.As<…, TurnPhaseSubSystem>()`; VContainer collects it into the list
  `TurnProcessorSystem` runs. The empty-list stub in `TurnInstaller` was removed when this phase landed.
- `Domains.Actions` now references `Turn` (+ `DefaultECS.Extensions`, `Core`, `VContainer`, `UniTask`) in
  addition to `Domains.Economy` + `Domains.Actors`.

Still scaffold: Mayor/Noble verbs, cross-domain turn scenarios (upkeep arithmetic, resolution, yield split),
and the other turn phases. The pre-existing `Configs/` classes are data only — no system consumes them yet.
