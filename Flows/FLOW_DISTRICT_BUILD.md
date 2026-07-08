---
category: A
read: trigger
trigger: "before changing any district-build event, system, transaction state — or any doc that retells this flow"
tags: [flow, district, cross-domain, ecs]
related:
  - "[BUILD_DISTRICT_ACTION](../Assets/Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md)"
  - "[DISTRICT_BUILD](../Assets/Presentation/UI/DistrictBuild/DISTRICT_BUILD.md)"
  - "[DISTRICT_OPEN_CONDITION](../Assets/Domains/Economy/DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
  - "[PATTERN_TRANSACTION_ENTITY](../Patterns/PATTERN_TRANSACTION_ENTITY.md)"
  - "[PATTERN_EVENT](../Patterns/PATTERN_EVENT.md)"
status: partial
code_refs:
  systems:          [DistrictBuildUISystem, DistrictBuildUISpawnSystem, BuildDistrictActionSystem, DistrictViewSpawnSystem]
  events:           [DistrictBuildRequestedEvent, DistrictBuildUIClosedEvent, DistrictBuildSelectedDistrictEvent, DistrictBuildConfirmedEvent, DistrictBuiltEvent]
  components:       [DistrictBuildSelectionComponent]
  tags:             [BuildDistrictActionTag]
---

# FLOW — District Build

The cross-domain contract of the district-build transaction: one player gesture (open → pick →
confirm / dismiss) spanning `Presentation.UI.DistrictBuild`, `Domains.Actions.BuildDistrictAction`,
`Domains.Economy`, `Presentation.Districts`.

## Purpose

A FLOW doc owns ONE cross-domain behavior end-to-end: its event vocabulary, state ownership, and
ordering invariants, stated as a CONTRACT — including target rules the code does not meet yet.
Module MDs link here and never retell the other side's half (retold halves are what rots). Live
wiring is the ecs-graph (`/ecs-graph`); this doc is diffable against it — a mismatch is either
drift to fix or a deliberate contract change.

```clojure
(def participants  ;; {assembly role-in-this-flow}
  {Presentation.UI.DistrictBuild       "projection + commands: renders the transaction, raises request pulses, owns NO transaction state"
   Domains.Actions.BuildDistrictAction "verb owner: creates the committed build entity directly on confirm — no draft"
   Domains.Economy                     "vocabulary (build configs, costs, open conditions) + TARGET home of the built-district fact"
   Presentation.Districts              "world view: spawns the district prefab for every built district"})
```

## Event vocabulary (the contract)

| Event | Home | Payload | Producer → Consumer | Semantics |
|---|---|---|---|---|
| `DistrictBuildRequestedEvent` | Presentation.UI | — | `HexInfoPanelView` → `DistrictBuildUISystem`, `DistrictBuildListUISubSystem` | player asked to open the overlay for the selected hex |
| `DistrictBuildUIClosedEvent` | Presentation.UI | — | `DistrictBuildUIView` → `DistrictBuildUISystem` | hide the overlay; PURE presentation |
| `DistrictBuildSelectedDistrictEvent` | Presentation.UI | — (payload-less) | `DistrictBuildListUISubSystem` → `DistrictBuildUISystem` | player picked a district row; sections re-reconcile |
| `DistrictBuildConfirmedEvent` | Domains.Actions | `HexCoord` + `DistrictType` | `DistrictBuildUIView` → `BuildDistrictActionSystem` | player committed: create the committed build entity directly (no draft) |
| `DistrictBuiltEvent` | Domains.Actions | `HexIdComponent` + `DistrictTypeComponent` siblings (target: payload-less) | `BuildDistrictActionSystem` → `DistrictViewSpawnSystem` | a district EXISTS; today raised at confirm (no ticking yet) |

```clojure
(def payload-verdicts  ;; deviations from PATTERN_EVENT that the contract removes
  {DistrictBuiltEvent {:siblings [HexIdComponent DistrictTypeComponent] :verdict :dead}})  ;; the sole consumer reconciles from world state and ignores them
```

## State ownership

```clojure
(def state-ownership  ;; {state {:now … :target …}} — the split below is the flow's core defect
  {:selected-hex    {:now "Presentation `HexSelectedComponent` — pre-confirm only; read into the committed entity's `HexIdComponent` at confirm, never owned by Actions before that"
                     :target :same}
   :chosen-district {:now    DistrictBuildSelectionComponent  ;; component in Presentation.UI.DistrictBuild.Components (own entity, tagged DistrictBuildSelectionTag); written by DistrictBuildListUISubSystem, read by the UI subsystems + DistrictBuildUIView (confirm payload)
                     :target :same}  ;; no draft to migrate onto — selection staying in Presentation IS the target (gap 1 retired, see Gap list)
   :payer           {:now    "view-local (DistrictBuildPriceUIView.SelectedOwner)"  ;; render-only today
                     :target "committed-entity component once resource-spend lands (gap 5)"}
   :built-district  {:now    "the committed verb entity itself (BuildDistrictActionTag), created directly at confirm"  ;; no domain fact exists; Presentation.Districts renders the VERB
                     :target "fact entity in Domains.Economy (gap 2)"}})
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:confirm-vs-dismiss  "OnConfirmClicked raises Confirmed then Close, in one click; OnCloseClicked/OnScrimClicked raise Close only"  ;; no draft, so dismiss never has anything to discard
   :list-before-others  "DistrictBuildListUISubSystem populates FIRST (Priority order)"})  ;; it default-writes the selection the other sections read — reorder = throw on a missing selection
```

## Target contract

The committed entity in `Domains.Actions` is created directly on confirm — no draft, no promote
step. The UI is a projection that reads current Presentation selection state (`HexSelectedComponent`
+ `DistrictBuildSelectionComponent`) and raises command pulses; a completed transaction writes the
built-district FACT into `Domains.Economy`; `Presentation.Districts` renders facts, never verbs.
`DistrictBuiltEvent` fires when the fact is written — which becomes "at construction completion"
once turn-ticking exists; today completion == confirm.

## Gap list — ordered backlog (one session each)

1. ~~**Selection moves onto the draft.**~~ MOOT (2026-07-08): the draft/template lifecycle was
   removed entirely. Selection stays in Presentation (`DistrictBuildSelectionComponent`); the
   chosen district reaches Actions via the `DistrictBuildConfirmedEvent` payload that
   `DistrictBuildUIView` composes at confirm. No entity-level migration needed.
2. **The built-district fact.** New Economy archetype (`DistrictTag` + `HexIdComponent` +
   `DistrictTypeComponent`); promotion (interim: == completion) writes the fact;
   `DistrictViewSpawnSystem` reconciles off the fact, not `BuildDistrictActionTag`;
   `DistrictBuiltEvent` becomes payload-less and fires at fact-write.
3. **Catalogue-lookup dedup.** One owner for "find config by DistrictType": today `TryGetDistrict`
   is verbatim ×2 (Price / HexResources subsystems), `TryGetCost` is the same shape,
   `ResolvePrefab` a third variant. Stateless helper in Economy District `Helpers/`.
4. ~~**Seam doc/comment reconciliation.**~~ DONE (2026-07-08): `BUILD_DISTRICT_ACTION.md` known-gap
   paragraph corrected (the confirm re-stamp is now documented); `DistrictViewSpawnSystem` code comment
   now names the emitter (`BuildDistrictActionSystem.RaiseDistrictBuilt`); `DistrictBuildRequestedEvent`
   xml-doc consumer corrected to `DistrictBuildUISystem`; `DISTRICT_BUILD.md` selection-ownership (the
   List subsystem owns `DistrictBuildSelectionComponent`) and payer (view-local) both corrected.
5. **Future (needs turn-ticking):** `DistrictBuiltEvent` fires at construction completion; payer
   moves from the view onto the committed entity when resource-spend lands.

## Current State

PARTIAL. Implemented: the overlay UI (orchestrator + 4 section subsystems), confirm creating the
committed build entity directly (no draft), live district-view spawn off that verb entity. The
contract above is the TARGET; gaps 2–5 are the remaining distance to it (gap 1 retired). Not built:
cost/AP spend, turn-ticking, outcome application, owner attribution.
