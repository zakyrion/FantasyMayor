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
  systems:          [BuildDistrictTemplateSpawnSystem, BuildDistrictActionSystem, BuildDistrictTemplateCancelSystem]
  components:       [ActionIdComponent]
  world_components: [ActionIdAllocatorComponent]
  tags:             [BuildDistrictActionTemplateTag, BuildDistrictActionTag]
  events:           [DistrictBuildStartedEvent, DistrictBuildConfirmedEvent, DistrictBuildCancelledEvent]
---

# Build District Action

The owner-scoped **build verb** — eventually turns an owner's confirmed choice into a live, multi-turn
construction that ticks each turn, completes, and applies the district's outcome. The current slice only
lands the draft→confirm→promote entity lifecycle below; tick/complete/outcome do not exist yet (see
Current State).

## Purpose
`Economy` owns the owner-agnostic district vocabulary (placement, unlock, cost, outcome — all keyed by
`DistrictType`, none knowing about an owner). This verb is the owner-scoped orchestration that CONSUMES
it: reads the cost, spends a specific owner's resources, ticks turns down, and joins the outcome
catalogue on completion. Orchestration, not vocabulary — the standing `Actions` rule (`ACTIONS.md`).

## Trigger
Three reactive systems, one per pulse of the draft lifecycle (full producer→consumer flow: the ecs-graph,
`/ecs-graph`):

```clojure
(def triggers  ;; {system {:on pulse :do effect}}
  {BuildDistrictTemplateSpawnSystem  {:on DistrictBuildStartedEvent   :do "spawn draft entity"}
   BuildDistrictActionSystem         {:on DistrictBuildConfirmedEvent :do "promote draft → committed"}
   BuildDistrictTemplateCancelSystem {:on DistrictBuildCancelledEvent :do "discard draft entity"}})
```

## Non-Obvious Invariants
```clojure
(def invariants
  {:draft-entity-while-overlay-open  "exactly one BuildDistrictActionTemplateTag entity"  ;; spawn open → discard cancel → promote confirm
   BuildDistrictActionSystem         {:confirm-no-draft :THROW}   ;; broken invariant, not a benign no-op
   BuildDistrictTemplateCancelSystem {:cancel-no-draft  :no-op}   ;; sanctioned quiet return
   DistrictBuildStartedEvent         "carries HexCoord + DistrictType"  ;; payload event — why: Design Decisions
   DistrictBuildCancelledEvent       :payload-less                ;; targets whichever entity carries the template tag
   :confirm<->cancel                 "DECOUPLED, never both"      ;; why: Design Decisions
   ActionIdAllocatorComponent        "world component, seeded Next=1 in BuildDistrictActionSystem ctor"})  ;; ids start at 1, 0 = unset
```

## Design Decisions
- **Payload event across an asmdef boundary.** `DistrictBuildStartedEvent` carries `HexCoord` +
  `DistrictType` because `Domains.Actions` cannot read Presentation state (`HexSelectedComponent` /
  `DistrictBuildSelectionComponent`): `Presentation.UI → Domains.Actions` already exists, so the reverse
  read would be a circular asmdef reference. `DistrictBuildCancelledEvent` stays payload-less — the
  Actions domain only needs to know the one draft goes away, not which one.
- **Confirm and cancel are deliberately decoupled pulses**, not two branches of one "close" event.
  `DistrictBuildUIView.OnConfirmClicked` raises confirm+close but never cancel, so the close pulse cannot
  discard the entity `BuildDistrictActionSystem` just promoted. `DistrictBuildClosedEvent`
  (Presentation-only) stays a pure "hide the window" concern — it never reaches the draft lifecycle.
- **Incremental rebuild, one mechanic at a time.** The prior big-bang verb tried to land cost + AP +
  owner + lifecycle + resource-spend together and was rolled back for it. This slice adds only
  draft-spawn + promote + cancel; cost, AP spend, turn-ticking, owner attribution, and outcome
  application remain separate future steps.

## Current State
PARTIAL. The draft→confirm→promote entity lifecycle is implemented:
- `BuildDistrictTemplateSpawnSystem` spawns the draft (`HexIdComponent` + `DistrictTypeComponent` +
  `BuildDistrictActionTemplateTag`) on `DistrictBuildStartedEvent`.
- `BuildDistrictActionSystem` promotes it on `DistrictBuildConfirmedEvent`: stamps a unique
  `ActionIdComponent` (via `ActionIdAllocatorComponent`, now live — no longer dormant), swaps
  `BuildDistrictActionTemplateTag` → `BuildDistrictActionTag`.
- `BuildDistrictTemplateCancelSystem` discards the draft on `DistrictBuildCancelledEvent`.

The earlier `BuildDistrictActionConfig` stub `ScriptableObject` is removed — no config-driven behavior
exists yet.

Known gap: the draft's `DistrictTypeComponent` is stamped from the current selection AT OPEN time —
usually `Unknown`, since the district is picked from the list AFTER the overlay opens. It is not
resynced when the player later changes the selection; that is a separate next step.

Not built: tick, complete, apply-outcome, any resource or AP spend, any owner attribution.
