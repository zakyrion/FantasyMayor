---
category: C
read: reference
tags:
  - plan
  - district
  - actions
  - wip
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[ACTIONS](Assets/Domains/Actions/ACTIONS.md)"
  - "[BUILD_DISTRICT_COST](Assets/Domains/Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](Assets/Domains/Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[BUILD_DISTRICT_ACTION](Assets/Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md)"
  - "[DISTRICT_OPEN_CONDITION](Assets/Domains/Economy/DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
---

# District-Build Action — Program Plan (FM-10)

> **WIP program doc.** Multi-session; delete when the program completes.
>
> **Scope (important).** This program builds the **technical Actions-side mechanic** — the `BuildDistrictAction`
> verb (draft → snapshot → commit → tick → complete → apply) and the `BuildDistrictOutcome` catalogue. The
> **DistrictBuild UI that drives it — the window, the buttons, and the raise-sites that fire the Actions events —
> is a SEPARATE task**, out of this program's scope. So the "final" here is a **complete-but-dormant** verb: it
> only goes live when the UI task adds the `Boot` composition + the UI raises. Judge this program as a technical
> foundation, not a playable feature.

## Context

`Economy.DistrictOpenCondition` answers "may this district type be built?"; `Economy.DistrictBuildCost` holds
"what does it cost?". This program adds the missing **build verb**: turning a confirmed choice into a live,
multi-turn construction that completes and produces a result. Two ECS families, DIFFERENT shapes:

1. **`BuildDistrictAction`** (Actions) — a live, one-row-per-pending-build action: draft on window-open, snapshot
   as the player picks, **commit** (spend AP/resources, start countdown) or **discard**, tick down, complete.
   Plain components/tags + reactive / turn-phase systems. NOT the catalogue pattern.
2. **`BuildDistrictOutcome`** (Economy) — an authored polymorphic catalogue (`PATTERN_POLYMORPHIC_CATALOGUE`,
   spawn-only): one outcome per `DistrictType`, materialized into an entity table. On completion the action's
   `DistrictType` joins it and the matching outcome runs (first: spawn City Center).

## Key decisions (settled — do not re-derive)
- **Boundary:** cost + outcome are owner-agnostic **Economy vocabulary** keyed by `DistrictType`; the Actions
  verb READS them, never owns them. Math in Economy, sequencing in Actions.
- **Naming:** district-first for the Economy catalogues (`DistrictBuildCost*`, `DistrictBuildOutcome*`);
  verb-first for the Actions verb (`BuildDistrictAction*`). Generic action-lifecycle components keep the singular
  `Action` prefix (shared action key space — the FK/PK exception).
- **Per-district outcome:** each district gets its OWN `Spawn<District>OutcomeConfig` + subsystem (Open-Closed).
  First kind `SpawnCityCenterOutcomeConfig`.
- **Trigger events are Actions-owned, raised by the UI** (`Domains.Actions` must not reference `Presentation.UI`).
  The snapshot event carries a `DistrictType` + `HexIdComponent` payload (`PATTERN_EVENT` tolerated payload).
- **Actions references `Domains.Map`** so the draft carries the real `HexIdComponent` FK (natively joinable to the
  hex table; a coordinate would not). No cycle — `Economy` already references `Map`.
- **Spend = per-owner (decision ii):** an Economy affordability query + spend with a City path and a Mayor path
  (Open/Closed by owner), each `AsMultiMap<ConcreteFK>`; home `Domains.Economy.Resource.*`, the spend counterpart
  of `ResourceLoadoutSpawner`. Fail-loud on unaffordable.
- **Addressable keys unchanged:** cost loads from the legacy `"ActionsDistrictsBuildConfig"`; outcome from
  `"DistrictBuildOutcomesConfig"`. Align Unity-side later.

## Status ledger
- ✅ **Step 1** — `Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md` extracted.
- ✅ **Step 2** — `BuildDistrictOutcome` catalogue (Economy, spawn-only). See `BUILD_DISTRICT_OUTCOME.md`.
- ✅ **Step 3** — `BuildDistrictAction` data structures. See `BUILD_DISTRICT_ACTION.md`.
- ✅ **Step 4a** — draft envelope: reactive spawn/discard + `ActionIdAllocatorComponent`. Dormant.
- ✅ **Step 4b** — snapshot system + payload event; writes `DistrictType` + `HexIdComponent` + cost snapshot. Dormant.
- ⬜ **Step 4c** — commit + spend (below).
- ⬜ **Step 4e** — tick + complete (below).
- ⬜ **Step 5** — apply outcome (below).
- ⬜ **UI task (SEPARATE)** — `Boot` composition of the Actions systems + the DistrictBuild raise-sites that fire
  draft-requested / snapshot / commit / discard. Makes the verb live. Out of this program's technical scope.

## ⬜ Step 4c — Commit + spend
On confirm the UI raises an Actions-owned commit pulse (`BuildDistrictActionCommitRequestedEvent`; the UI task
wires the raise). An Actions verb system reacts: attach the payer owner-FK (`CityIdComponent`/`MayorIdComponent`)
to the draft, validate affordability, **deduct** AP + resources via the new **Economy** per-owner spend
(City/Mayor paths, Open/Closed), then seed `ActionTurnLeftComponent` from `DistrictBuildCostConfig.TurnsToBuild`.
Affordability failure fails loud. (The owner-FK surfacing from the UI's payer choice is resolved here, not in 4b.)

## ⬜ Step 4e — Tick + complete
An Actions `TurnPhaseSubSystem` decrements `ActionTurnLeftComponent` for every committed action each turn; at zero
it Sets `ActionCompleteTag`. Register `.As<TurnPhaseSubSystem>()`; priority ~550 (after AP-restore 500, before
unlock-eval 1000). Reuse the `Turn` pipeline (TURN.md / TURN_PHASES.md).

## ⬜ Step 5 — Apply outcome (complete → join → spawn City Center)
Reactive on `ActionCompleteTag`: read the completed action's `DistrictType` → `AsMap<DistrictTypeComponent>` join
into the `DistrictBuildOutcome` table (Economy) → run the matching subsystem. First concrete:
`SpawnCityCenterOutcomeSubSystem` spawns the City Center district entity (into the `District` table). Then dispose
the completed action. **Open:** whether the join+spawn lives in Actions or Economy (the outcome table is Economy,
the trigger is Actions) — decide at build time. This also makes the `DistrictSingleOpenCondition` evaluator
meaningful (once a CityCenter exists, its `DistrictCanBeBuildTag` flips off).

## Per-slice hygiene
Each slice: build per the `Patterns/PATTERN_*.md` recipes; after code lands, STOP and ask before docs/graph sync
(refresh ecs-graph + di-graph, re-run `gen_index.py`). `.cs` reads-for-editing are budgeted — delegate discovery
to the read-only scouts.
