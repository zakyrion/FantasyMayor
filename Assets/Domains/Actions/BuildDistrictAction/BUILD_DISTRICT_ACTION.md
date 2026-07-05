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
  - "[PATTERN_REACTIVE_SYSTEM](../../../../Patterns/PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_EVENT](../../../../Patterns/PATTERN_EVENT.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](../../../../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
status: scaffold
code_refs:
  configs: [BuildDistrictActionConfig]
---

# Build District Action

The owner-scoped **build verb** — eventually turns an owner's confirmed choice into a live, multi-turn
construction that ticks each turn, completes, and applies the district's outcome. That target behavior
does not exist yet (see Current State).

## Purpose (target, not current reality)
`Economy` owns the owner-agnostic district vocabulary (placement, unlock, cost, outcome — all keyed by
`DistrictType`, none knowing about an owner). This verb is meant to be the owner-scoped orchestration
that CONSUMES it: reads the cost, spends a specific owner's resources, ticks turns down, and joins the
outcome catalogue on completion. Orchestration, not vocabulary — the standing `Actions` rule
(`ACTIONS.md`).

## Current State
SCAFFOLD. The previous draft→snapshot→commit implementation (4 systems, 4 events, the AP-cost/turn-left
components, the lifecycle tags, and the whole `ResourceSpend/` resource-spend mechanic) was judged
badly-done and has been **rolled back to a bare base** — none of that code exists anymore. Strategic
pivot: rebuild the verb in very small ECS steps, layering one mechanic at a time (starting with no
resource cost, no Action Points, no owner), instead of building the whole system at once.

What remains:
- `BuildDistrictActionConfig` — a stub `ScriptableObject` holding only a `DistrictType`. No systems
  consume it yet.
- The shared `ActionIdComponent` / `ActionIdAllocatorComponent` identity scaffold (see `ACTIONS.md`) is
  kept but dormant — nothing in this sub-domain writes or reads them right now.

Not built (everything): draft/spawn, snapshot, commit, tick, complete, apply-outcome, any resource or AP
spend, any owner attribution.

## Design Decisions
- **Incremental rebuild, one mechanic at a time.** The prior big-bang verb tried to land cost + AP +
  owner + lifecycle + resource-spend together and was rolled back for it. The next version adds
  complexity in small, individually reviewable ECS steps, starting from the bare verb with none of that.
