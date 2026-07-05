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
status: partial
code_refs:
  components: [ActionIdComponent, ActionIdAllocatorComponent, ActionAPCostComponent, DistrictBuildCostResourcePriceComponent, BuildDistrictActionTurnLeftComponent]
  tags:       [BuildDistrictActionTag, BuildDistrictActionCommittedTag, ActionCompleteTag]
  events:     [BuildDistrictActionDraftRequestedEvent, BuildDistrictActionDiscardRequestedEvent, BuildDistrictActionSnapshotRequestedEvent, BuildDistrictActionCommitRequestedEvent]
  systems:    [BuildDistrictDraftSpawnSystem, BuildDistrictDraftDiscardSystem, BuildDistrictActionSnapshotSystem, BuildDistrictActionCommitSystem, ResourceSpender]
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
PARTIAL, and still deliberately **technical, not a usable feature yet** — the Actions-side mechanic is
being built ahead of the UI that will drive it. Everything here is **dormant**: the systems are
registered in `ActionsInstaller`, but the events that trigger them are never raised by any UI, and none
of it is composed into `Boot`'s Gameplay loop yet.

Built (Actions-side, dormant):
- **Draft lifecycle** — `BuildDistrictDraftSpawnSystem` reconciles to exactly one draft (allocates the
  `ActionIdComponent` PK via the `ActionIdAllocatorComponent` world singleton — now shared under
  `Actions/Components/`, not BuildDistrictAction-specific — sets `BuildDistrictActionTag`) on a
  draft-requested pulse; `BuildDistrictDraftDiscardSystem` disposes it on a discard-requested pulse.
  Reactive (`PATTERN_REACTIVE_SYSTEM`), one event each — the create/destroy split.
- **Snapshot** — `BuildDistrictActionSnapshotSystem` reacts to `BuildDistrictActionSnapshotRequestedEvent`
  (payload: `DistrictType` + target `HexIdComponent`) and writes onto the draft: `DistrictTypeComponent`,
  `HexIdComponent` (FK), and the frozen cost — `ActionAPCostComponent` + `DistrictBuildCostResourcePriceComponent`
  joined from `DistrictBuildCostsConfig` (Economy) by `DistrictType`. Fails loud on no draft / no
  catalogue / unknown district.
- **Commit** — `BuildDistrictActionCommitSystem` reacts to `BuildDistrictActionCommitRequestedEvent`
  (payload: paying `ActorType` + owner id). Spends AP + resources through the new `ResourceSpender`
  orchestrator FIRST (atomic, fail-loud — an unaffordable commit throws before the draft is touched),
  then stamps the payer owner FK (`CityIdComponent`/`MayorIdComponent`), seeds
  `BuildDistrictActionTurnLeftComponent` from the cost catalogue's turns-to-build, and sets
  `BuildDistrictActionCommittedTag`.

Not built: tick+complete (decrement `BuildDistrictActionTurnLeftComponent`, set `ActionCompleteTag` at
zero), apply-outcome (join `DistrictBuildOutcome`, spawn City Center).

## Public Contract & Gotchas
- **Actions-owned trigger events, raised by the UI.** `Domains.Actions` does NOT reference `Presentation.UI`, so
  the lifecycle triggers are Actions-owned events (`Events/`); the UI (which references Actions) raises them. The
  snapshot event carries a small identifying payload (`DistrictType` + `HexIdComponent`) because the player's
  choice originates in UI/selection state Actions cannot read — the `PATTERN_EVENT` tolerated-payload case. The
  commit event's payload is the paying `ActorType` + owner id — the player's payer choice, same tolerated case.
- **Actions references `Domains.Map`** (asmdef) so the draft carries the real `HexIdComponent` FK (the hex
  entity's PK). That makes the draft natively joinable to the hex table (`AsMap<HexIdComponent>`) at apply time; a
  bare coordinate would not join. No cycle — `Economy` already references `Map`. (`DistrictType` is likewise the
  join key into the Economy catalogues.)
- **One draft at a time.** The draft is a singleton — spawn guards on the current draft set, discard disposes it.
  Priorities: spawn 560, discard 561, snapshot 563, commit 565 (all before `EventCleanupSystem`).
- **The cost is a frozen snapshot**, not a live read — the draft is a durable pending record; snapshotting at edit
  time isolates it from later config changes and enables refund-on-discard down the line.
- **Commit spends before it mutates the draft.** `ResourceSpender` (`ResourceSpend/`) checks affordability for
  BOTH the AP cost and the resource cost before deducting either, and deducts only if both succeed — an
  unaffordable or unroutable commit throws and leaves the draft untouched. AP is always paid by the mayor (the
  game's sole Action-Point pool) regardless of which owner (`City` or `Mayor`) pays the resource cost; the
  arithmetic itself is Economy's `ResourceLedger` — `ResourceSpender` only resolves whose stacks to hand it.
- **`ActionIdComponent` / `ActionIdAllocatorComponent` are shared, not BuildDistrictAction-owned.** They now live
  in `Assets/Domains/Actions/Components/` as the generic action-identity PK + allocator for any action verb, not
  a BuildDistrictAction-specific concept — read this before assuming they belong to this sub-domain.

## Full Archetype (assembled once tick+complete land)
`ActionIdComponent` (PK) + `BuildDistrictActionTag` (discriminator) + `DistrictTypeComponent` (FK) +
`HexIdComponent` (FK) + `ActionAPCostComponent` + `DistrictBuildCostResourcePriceComponent`, then at
commit: owner-FK (`CityIdComponent`/`MayorIdComponent`) + `BuildDistrictActionTurnLeftComponent` +
`BuildDistrictActionCommittedTag` [+ `ActionCompleteTag` when tick reaches zero]. Refresh the ecs-graph
as each piece materializes.