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
  systems:          [DistrictBuildUISystem, DistrictBuildUISpawnSystem, BuildDistrictTemplateSpawnSystem, BuildDistrictActionSystem, BuildDistrictTemplateCancelSystem, DistrictViewSpawnSystem]
  events:           [DistrictBuildRequestedEvent, DistrictBuildClosedEvent, DistrictBuildSelectionRequestedEvent, DistrictBuildStartedEvent, DistrictBuildConfirmedEvent, DistrictBuildCancelledEvent, DistrictBuiltEvent]
  world_components: [DistrictBuildSelectionComponent]
  tags:             [BuildDistrictActionTemplateTag, BuildDistrictActionTag]
---

# FLOW — District Build

The cross-domain contract of the district-build transaction: one player gesture (open → pick →
confirm / cancel) spanning `Presentation.UI.DistrictBuild`, `Domains.Actions.BuildDistrictAction`,
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
   Domains.Actions.BuildDistrictAction "transaction owner: the draft/committed build entity and its lifecycle"
   Domains.Economy                     "vocabulary (build configs, costs, open conditions) + TARGET home of the built-district fact"
   Presentation.Districts              "world view: spawns the district prefab for every built district"})
```

## Event vocabulary (the contract)

| Event | Home | Payload | Producer → Consumer | Semantics |
|---|---|---|---|---|
| `DistrictBuildRequestedEvent` | Presentation.UI | — | `HexInfoPanelView` → `DistrictBuildUISystem`, `DistrictBuildListUISubSystem` | player asked to open the overlay for the selected hex |
| `DistrictBuildClosedEvent` | Presentation.UI | — | `DistrictBuildUIView` → `DistrictBuildUISystem` | hide the overlay; PURE presentation — must never reach the draft lifecycle |
| `DistrictBuildSelectionRequestedEvent` | Presentation.UI | — (target: identifying `DistrictType`) | `DistrictBuildListUISubSystem` → `DistrictBuildUISystem` (target: + a verb-domain updater) | player picked a district row; sections re-reconcile |
| `DistrictBuildStartedEvent` | Domains.Actions | `HexCoord` + `DistrictType` (target: `HexCoord` only) | `DistrictBuildUISystem` → `BuildDistrictTemplateSpawnSystem` | the transaction opened: spawn the draft entity |
| `DistrictBuildConfirmedEvent` | Domains.Actions | — | `DistrictBuildUIView` → `BuildDistrictActionSystem` | player committed: promote the draft |
| `DistrictBuildCancelledEvent` | Domains.Actions | — | `DistrictBuildUIView` → `BuildDistrictTemplateCancelSystem` | player dismissed: discard the draft |
| `DistrictBuiltEvent` | Domains.Actions | `HexIdComponent` + `DistrictTypeComponent` siblings (target: payload-less) | `BuildDistrictActionSystem` → `DistrictViewSpawnSystem` | a district EXISTS; today raised at confirm (no ticking yet) |

```clojure
(def payload-verdicts  ;; deviations from PATTERN_EVENT that the contract removes
  {DistrictBuildStartedEvent            {:field DistrictType :verdict :dead}  ;; stale at raise (see ordering) AND overwritten at confirm — carry HexCoord only
   DistrictBuiltEvent                   {:siblings [HexIdComponent DistrictTypeComponent] :verdict :dead}  ;; the sole consumer reconciles from world state and ignores them
   DistrictBuildSelectionRequestedEvent {:target "tiny identifying DistrictType"}})  ;; becomes the selection COMMAND once the draft is the source of truth (gap 1)
```

## State ownership

```clojure
(def state-ownership  ;; {state {:now … :target …}} — the split below is the flow's core defect
  {:hex-under-build {:now    "draft entity HexIdComponent (Actions)"
                     :target :same}
   :chosen-district {:now    DistrictBuildSelectionComponent  ;; world component in Domains.Economy.District — placed in the substrate ONLY so Actions can read it; written by Presentation (DistrictBuildListUISubSystem), read by the UI subsystems + BuildDistrictActionSystem; the draft's own DistrictTypeComponent is a STALE copy until confirm re-stamps it
                     :target "draft entity DistrictTypeComponent — the ONE home; the Economy component is deleted"}
   :payer           {:now    "view-local (DistrictBuildPriceUIView.SelectedOwner)"  ;; render-only today
                     :target "draft entity component once resource-spend lands (gap 5)"}
   :built-district  {:now    "the promoted verb entity itself (BuildDistrictActionTag)"  ;; no domain fact exists; Presentation.Districts renders the VERB
                     :target "fact entity in Domains.Economy (gap 2)"}})
```

## Ordering invariants

```clojure
(def ordering-invariants
  {:confirm<->cancel        "DECOUPLED, never both"                  ;; DistrictBuildUIView.OnConfirmClicked raises confirm+close, never cancel — a promoted entity must survive the close
   :closed-event            {:must-not "reach the draft lifecycle"}  ;; hide-only; cancel is the discard
   :list-before-others      "DistrictBuildListUISubSystem populates FIRST (Priority order)"  ;; it default-writes the selection the other sections read — reorder = throw on a missing selection
   :started-before-populate "RaiseStarted precedes PopulateSections in DistrictBuildUISystem.Open"  ;; hence the draft's district stamp is stale-by-design; harmless ONLY because confirm re-stamps (BuildDistrictActionSystem) — removed by gap 1
   :confirm-no-draft        :THROW                                   ;; broken invariant (BUILD_DISTRICT_ACTION)
   :cancel-no-draft         :no-op})                                 ;; sanctioned quiet return
```

## Target contract

The flow follows `PATTERN_TRANSACTION_ENTITY`: the draft entity in `Domains.Actions` is the ONE
source of truth for all transaction state; the UI is a projection that reads the entity
(Presentation → Domains is the allowed direction) and raises command pulses; a completed
transaction writes the built-district FACT into `Domains.Economy`; `Presentation.Districts`
renders facts, never verbs. `DistrictBuiltEvent` fires when the fact is written — which becomes
"at construction completion" once turn-ticking exists; today completion == confirm.

## Gap list — ordered backlog (one session each)

1. **Selection moves onto the draft.** `DistrictBuildSelectionRequestedEvent` gains an identifying
   `DistrictType` payload; a new small reactive system in Actions `Set()`s the draft's
   `DistrictTypeComponent`; UI sections read the draft; delete `DistrictBuildSelectionComponent`
   from Economy; drop the confirm re-stamp in `BuildDistrictActionSystem`;
   `DistrictBuildStartedEvent` drops `Type`.
2. **The built-district fact.** New Economy archetype (`DistrictTag` + `HexIdComponent` +
   `DistrictTypeComponent`); promotion (interim: == completion) writes the fact;
   `DistrictViewSpawnSystem` reconciles off the fact, not `BuildDistrictActionTag`;
   `DistrictBuiltEvent` becomes payload-less and fires at fact-write.
3. **Catalogue-lookup dedup.** One owner for "find config by DistrictType": today `TryGetDistrict`
   is verbatim ×2 (Price / HexResources subsystems), `TryGetCost` is the same shape,
   `ResolvePrefab` a third variant. Stateless helper in Economy District `Helpers/`.
4. **Seam doc/comment reconciliation** (docs-curator brief; the user schedules it):
   `BUILD_DISTRICT_ACTION.md` known-gap paragraph (the confirm re-stamp EXISTS — doc behind code);
   `DistrictViewSpawnSystem` "no emitter raises this pulse yet" (the emitter exists);
   `DistrictBuildRequestedEvent` xml-doc names a wrong consumer; `DISTRICT_BUILD.md`
   "orchestrator maintains the ECS selection" (the List subsystem owns it) + payer described as
   subsystem-local (it is view-local).
5. **Future (needs turn-ticking):** `DistrictBuiltEvent` fires at construction completion; payer
   moves from the view onto the draft when resource-spend lands.

## Current State

PARTIAL. Implemented: the overlay UI (orchestrator + 4 section subsystems), the draft →
confirm/cancel → promote lifecycle, live district-view spawn off the promoted verb entity. The
contract above is the TARGET; gaps 1–5 are the ordered distance to it. Not built: cost/AP spend,
turn-ticking, outcome application, owner attribution.
