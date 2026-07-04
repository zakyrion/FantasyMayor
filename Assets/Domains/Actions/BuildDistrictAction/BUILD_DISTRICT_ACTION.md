---
category: A
read: reference
tags: [actions, district, ecs, verb]
related:
  - "[ACTIONS](../ACTIONS.md)"
  - "[BUILD_DISTRICT_COST](../../Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](../../Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[DISTRICT_BUILD_ACTION_PLAN](../../../../DISTRICT_BUILD_ACTION_PLAN.md)"
  - "[PATTERN_REACTIVE_SYSTEM](../../../../Patterns/PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_EVENT](../../../../Patterns/PATTERN_EVENT.md)"
status: partial
code_refs:
  components: [ActionIdComponent, ActionIdAllocatorComponent, ActionAPCostComponent, ActionResourcePriceComponent, ActionTurnLeftComponent]
  tags:       [BuildDistrictActionTag, ActionCompleteTag]
  events:     [BuildDistrictActionDraftRequestedEvent, BuildDistrictActionDiscardRequestedEvent, BuildDistrictActionSnapshotRequestedEvent]
  systems:    [BuildDistrictDraftSpawnSystem, BuildDistrictDraftDiscardSystem, BuildDistrictActionSnapshotSystem]
---

# Build District Action

The owner-scoped **build verb**: turns an owner's confirmed choice into a live, multi-turn construction that
ticks each turn, completes, and applies the district's outcome.

## Purpose
`Economy` owns the owner-agnostic district vocabulary (placement, unlock, cost, outcome — all keyed by
`DistrictType`, none knowing about an owner). This verb is the owner-scoped orchestration that CONSUMES it: it
reads the cost, spends a specific owner's resources, ticks turns down, and joins the outcome catalogue on
completion. Orchestration, not vocabulary — the standing `Actions` rule (`ACTIONS.md`).

## Current State
PARTIAL, and deliberately **technical, not a usable feature yet** — the Actions-side mechanic is being built
ahead of the UI that will drive it. **The DistrictBuild UI (window + the raise-sites that fire these events) is a
SEPARATE task** (see the plan). Until it lands everything here is **dormant**: the systems are registered in
`ActionsInstaller` but the events that trigger them are never raised, and they are not yet composed into `Boot`'s
Gameplay loop.

Built (Actions-side, dormant):
- **Draft lifecycle** — `BuildDistrictDraftSpawnSystem` reconciles to exactly one draft (allocates the
  `ActionIdComponent` PK via the `ActionIdAllocatorComponent` world singleton, sets `BuildDistrictActionTag`) on a
  draft-requested pulse; `BuildDistrictDraftDiscardSystem` disposes it on a discard-requested pulse. Reactive
  (`PATTERN_REACTIVE_SYSTEM`), one event each — the create/destroy split.
- **Snapshot** — `BuildDistrictActionSnapshotSystem` reacts to `BuildDistrictActionSnapshotRequestedEvent`
  (payload: `DistrictType` + target `HexIdComponent`) and writes onto the draft: `DistrictTypeComponent`,
  `HexIdComponent` (FK), and the frozen cost — `ActionAPCostComponent` + `ActionResourcePriceComponent` joined
  from `DistrictBuildCostsConfig` (Economy) by `DistrictType`. Fails loud on no draft / no catalogue / unknown
  district.

Not built: commit (spend AP+resources, seed `ActionTurnLeftComponent`), tick+complete (`ActionCompleteTag`),
apply-outcome (join `DistrictBuildOutcome`, spawn City Center). See the plan's remaining steps.

## Public Contract & Gotchas
- **Actions-owned trigger events, raised by the UI.** `Domains.Actions` does NOT reference `Presentation.UI`, so
  the lifecycle triggers are Actions-owned events (`Events/`); the UI (which references Actions) raises them. The
  snapshot event carries a small identifying payload (`DistrictType` + `HexIdComponent`) because the player's
  choice originates in UI/selection state Actions cannot read — the `PATTERN_EVENT` tolerated-payload case.
- **Actions references `Domains.Map`** (asmdef) so the draft carries the real `HexIdComponent` FK (the hex
  entity's PK). That makes the draft natively joinable to the hex table (`AsMap<HexIdComponent>`) at apply time; a
  bare coordinate would not join. No cycle — `Economy` already references `Map`. (`DistrictType` is likewise the
  join key into the Economy catalogues.)
- **One draft at a time.** The draft is a singleton — spawn guards on the current draft set, discard disposes it.
  Priorities: spawn 560, discard 561, snapshot 563 (all before `EventCleanupSystem`).
- **The cost is a frozen snapshot**, not a live read — the draft is a durable pending record; snapshotting at edit
  time isolates it from later config changes and enables refund-on-discard down the line.

## Full Archetype (assembled once commit/tick land)
`ActionIdComponent` (PK) + `BuildDistrictActionTag` (discriminator) + `DistrictTypeComponent` (FK) +
`HexIdComponent` (FK) + owner-FK (`CityIdComponent`/`MayorIdComponent`, added at commit) + `ActionAPCostComponent`
+ `ActionResourcePriceComponent` + `ActionTurnLeftComponent` [+ `ActionCompleteTag` when done]. Refresh the
ecs-graph as each piece materializes.
