---
category: A
read: reference
tags:
  - actions
  - district
  - ecs
  - verb
related:
  - "[ACTIONS](../ACTIONS.md)"
  - "[BUILD_DISTRICT_COST](../../Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](../../Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[DISTRICT_BUILD](../../../Presentation/UI/DistrictBuild/DISTRICT_BUILD.md)"
  - "[PATTERN_REACTIVE_SYSTEM](../../../../Patterns/PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_EVENT](../../../../Patterns/PATTERN_EVENT.md)"
status: partial
code_refs:
  systems:          [BuildDistrictActionSystem]
  components:       [ActionIdComponent]
  world_components: [ActionIdAllocatorComponent]
  tags:             [BuildDistrictActionTag]
  events:           [DistrictBuildConfirmedEvent]
---

# Build District Action

The owner-scoped **build verb** — eventually turns an owner's confirmed choice into a live, multi-turn
construction that ticks each turn, completes, and applies the district's outcome. The current slice only
creates the committed build entity directly on confirm (see below); tick/complete/outcome do not exist yet
(see Current State).

## Purpose
`Economy` owns the owner-agnostic district vocabulary (placement, unlock, cost, outcome — all keyed by
`DistrictType`, none knowing about an owner). This verb is the owner-scoped orchestration that CONSUMES
it: reads the cost, spends a specific owner's resources, ticks turns down, and joins the outcome
catalogue on completion. Orchestration, not vocabulary — the standing `Actions` rule (`ACTIONS.md`).

## Trigger
One reactive system (full producer→consumer flow: the ecs-graph, `/ecs-graph`):

```clojure
(def triggers  ;; {system {:on pulse :do effect}}
  {BuildDistrictActionSystem {:on DistrictBuildConfirmedEvent :do "create committed build entity directly"}})
```

## Non-Obvious Invariants
```clojure
(def invariants
  {DistrictBuildConfirmedEvent "carries HexCoord + DistrictType"  ;; payload event — why: Design Decisions
   ActionIdAllocatorComponent  "world component, seeded Next=1 in BuildDistrictActionSystem ctor"})  ;; ids start at 1, 0 = unset
```

## Design Decisions
- **No draft entity.** An earlier slice spawned a draft on overlay-open and promoted/discarded it on
  confirm/cancel. With no cost/AP-spend and no on-map preview, the draft owned nothing worth
  pre-spawning — it only added a stale `DistrictTypeComponent` (stamped at open, re-stamped at confirm)
  and a cancel-discard path. `BuildDistrictActionSystem` now creates the committed entity directly on
  confirm; building directly is simpler and equivalent.
- **Payload event across an asmdef boundary.** `DistrictBuildConfirmedEvent` carries `HexCoord` +
  `DistrictType` because `Domains.Actions` cannot read Presentation state (`HexSelectedComponent` /
  `DistrictBuildSelectionComponent`): `Presentation.UI → Domains.Actions` already exists, so the reverse
  read would be a circular asmdef reference. `DistrictBuildUIView` composes the payload by reading both
  components via one-shot `EntitySet`s at confirm.
- **Confirm and close are separate pulses**, not two branches of one event.
  `DistrictBuildUIView.OnConfirmClicked` raises `DistrictBuildConfirmedEvent` then
  `DistrictBuildUIClosedEvent`; `BuildDistrictActionSystem` reacts only to the former — the latter is a
  pure Presentation hide, never routed through Actions.
- **Incremental rebuild, one mechanic at a time.** The prior big-bang verb tried to land cost + AP +
  owner + lifecycle + resource-spend together and was rolled back for it. This slice creates the
  committed entity directly on confirm; cost, AP spend, turn-ticking, owner attribution, and outcome
  application remain separate future steps.

## Current State
PARTIAL. `BuildDistrictActionSystem` creates the committed build entity directly on
`DistrictBuildConfirmedEvent`: stamps `HexIdComponent` + `DistrictTypeComponent` from the payload, a
unique `ActionIdComponent` (via `ActionIdAllocatorComponent`), and `BuildDistrictActionTag`. There is no
draft — no spawn, no promote, no cancel/discard path.

The earlier `BuildDistrictActionConfig` stub `ScriptableObject` is removed — no config-driven behavior
exists yet.

Not built: tick, complete, apply-outcome, any resource or AP spend, any owner attribution.
