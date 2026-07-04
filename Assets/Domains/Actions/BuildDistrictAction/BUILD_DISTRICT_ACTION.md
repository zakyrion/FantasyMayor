---
category: A
read: reference
tags: [actions, district, ecs, verb]
related:
  - "[ACTIONS](../ACTIONS.md)"
  - "[BUILD_DISTRICT_COST](../../Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](../../Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[DISTRICT_BUILD_ACTION_PLAN](../../../../DISTRICT_BUILD_ACTION_PLAN.md)"
status: partial
code_refs:
  components: [ActionIdComponent, ActionAPCostComponent, ActionResourcePriceComponent, ActionTurnLeftComponent]
  tags:       [BuildDistrictActionTag, ActionCompleteTag]
---

# Build District Action

The owner-scoped **build verb**: turns an owner's confirmed choice into a live, multi-turn construction that
ticks each turn, completes, and applies the district's outcome.

## Purpose
This IS the reason the cost and outcome catalogues are READ here but OWNED by `Economy`. `Economy` holds the
owner-agnostic district vocabulary (placement, unlock, cost, outcome — all keyed by `DistrictType`, none
knowing about an owner); this verb is the owner-scoped orchestration that consumes it: it reads the cost, spends
a specific owner's resources, ticks turns down, and joins the outcome catalogue on completion. Orchestration,
not vocabulary — the standing `Actions` rule (`ACTIONS.md`: verbs/orchestration here, mechanics in the domains).

## Current State
PARTIAL — **DATA STRUCTURES ONLY, no systems yet.** The sub-domain ships the action's live-lifecycle
components/tags and nothing else. There is no draft/commit/tick/complete system, no spending, no outcome apply.

Implemented (data only):
- `ActionIdComponent` — PK; `{ int Value }`, `IEquatable`. (Shared action key space — keeps the `Action`
  concept prefix, the FK/PK naming exception. ID **allocation** is a future system concern; here just the type.)
- `BuildDistrictActionTag` — discriminator ("this row is a build-district action").
- `ActionAPCostComponent` — `{ int }` AP price snapshot.
- `ActionResourcePriceComponent` — **zero-alloc** fixed inline struct: 7 named `ResourceComponent` slots +
  `Count` (used slots) + a `this[int]` indexer. No lists/arrays — the zero-alloc translation of the config's
  `List<ResourceComponent> DistrictPrices`.
- `ActionTurnLeftComponent` — `{ int }` turns remaining until finished.
- `ActionCompleteTag` — "completed; apply the outcome now".

## Planned Lifecycle (FUTURE — systems not built)
Draft → edit → commit / discard → tick → complete. The **mechanic lives in this Actions domain**, not in the
DistrictBuild view (`ACTIONS.md`); the UI only orchestrates.
- **Draft** — a draft action entity is born when the DistrictBuild window opens (Set `ActionIdComponent` +
  `BuildDistrictActionTag`).
- **Edit** — as the player picks district / hex / owner, the DistrictBuild view-subsystems `Set` their own FK
  components on the draft and **snapshot** the cost from `DistrictBuildCostsConfig` (Economy, joined by
  `DistrictType`) into `ActionAPCostComponent` + `ActionResourcePriceComponent`.
- **Commit** — on confirm, an Actions verb system validates affordability, deducts AP + resources from the
  owner's inventory (math stays owner-agnostic in Economy, sequencing here), and seeds `ActionTurnLeftComponent`
  from the config's `TurnsToBuild`.
- **Discard** — on close without confirm, the draft entity is disposed.
- **Tick + complete** — an Actions `TurnPhaseSubSystem` decrements `ActionTurnLeftComponent` each turn; at zero
  it Sets `ActionCompleteTag`. On completion the action's `DistrictType` **joins** the `DistrictBuildOutcome`
  table (Economy) and the matching outcome runs (first: spawn City Center).

## Future Archetype (assembled once the lifecycle systems land)
`ActionIdComponent` (PK) + `BuildDistrictActionTag` (discriminator) + `HexIdComponent` (FK) +
`DistrictTypeComponent` (FK) + owner-FK (City/Mayor/Noble id) + `ActionAPCostComponent` +
`ActionResourcePriceComponent` + `ActionTurnLeftComponent` [+ `ActionCompleteTag` when done]. Refresh the
ecs-graph when this materializes in code.
