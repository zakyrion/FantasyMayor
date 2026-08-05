---
category: B
read: trigger
trigger: "before building any multi-step behavior that spans more than one subdomain (a cross-domain transaction)"
tags: [pattern, ecs, cross-domain, transaction]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_TAG](PATTERN_TAG.md)"
  - "[PATTERN_VIEW_SYSTEM](PATTERN_VIEW_SYSTEM.md)"
  - "[FLOW_DISTRICT_BUILD](../Flows/FLOW_DISTRICT_BUILD.md)"
---

# Pattern — Transaction Entity (cross-domain behavior)

A multi-step behavior that spans subdomains gets exactly ONE home: a **transaction entity** in the
verb domain. All session state rides on that entity; the UI projects it and raises commands;
completion writes a domain FACT. This is the DDD saga expressed as a DoD row — no coordinator
class, an entity with a tag lifecycle. One approach.

## When

```clojure
(cond
  (and (>= subdomains 2) (multi-step? behavior)) transaction-entity       ;; open session state between pulses → this pattern
  (>= subdomains 2)                              PATTERN_REACTIVE_SYSTEM  ;; a single pulse crossing a boundary needs no session home
  :else                                          "ordinary single-domain patterns")
```

"Multi-step" = between the opening pulse and the committing pulse the outcome can still change —
i.e. there IS session state someone must own.

**Degenerate case (real precedent: district build).** If it turns out NOTHING worth owning exists
between open and commit (no cost spent, no on-map preview, selection is pure UI state), the draft
stage is dead weight — commit directly on the confirming pulse (a payload event composed by the UI)
and skip stages 1–3 below. The district-build flow made exactly this cut: its earlier draft/template
lifecycle was removed, `BuildDistrictActionSystem` creates the committed entity straight from
`DistrictBuildConfirmedEvent`. Reach for the full lifecycle only when the session state is real.

## Skeleton

The verb domain owns the entity and EVERY write to it. The transaction is ONE archetype carrying every
column it will ever need — including its stage column — so no stage change ever migrates the row
(ECS_CONVENTIONS → Birth Completeness).

Placeholder names (`MyAction*`) — substitute your verb's:

```csharp
// 1. Open — a reactive system in the VERB domain creates the row BY its archetype.
var session = ActionsArchetypes.MyAction(_world).CreateEntity();   // every column present at default
session.AddComponent(new HexIdComponent { Coords = opened.Coords });          // identifying state
session.AddComponent(new MyChoiceComponent { Value = default });              // session state, owned HERE
session.AddComponent(new MyStageComponent { Value = MyStage.Draft });         // stage = a COLUMN

// 2. Mutate — every change arrives as a COMMAND pulse (tiny identifying payload, PATTERN_EVENT)
//    consumed by a small reactive system in the verb domain; the UI never writes the entity.
protected override void Update(GameState state, in Entity pulse)
{
    if (!EcsEventExtensions.IsRipe(pulse))
        return;

    var picked = pulse.GetComponent<MyChoicePickedEvent>().Value;
    if (_sessions.TryGetFirst(out var session))
        session.AddComponent(new MyChoiceComponent { Value = picked });
}

// 3. Commit — advance the stage column; the row already carries the full, live transaction state.
session.AddComponent(new MyStageComponent { Value = MyStage.Committed });

// 4. Complete — write the FACT into the substrate domain, then a payload-less pulse.
var fact = EconomyArchetypes.MyFact(_world).CreateEntity();       // the substrate's own table
fact.AddComponent(new HexIdComponent { Coords = hex.Coords });
fact.AddComponent(new MyResultComponent { Value = result });

_world.CreateEvent(new MyFactCreatedEvent());                     // consumers reconcile from the fact table
```

The UI side is a projection: its subsystems read the transaction entity directly
(Presentation → Domains is the allowed asmdef direction) and reconcile idempotently; user input
becomes command pulses, never writes.

## Rules

```clojure
(def transaction-entity-rules
  {:home          "verb domain (Actions)"                     ;; the verb owns its session — never the substrate, never Presentation
   :state         "ALL transaction state on the ONE entity"   ;; no world-component copies, no second home; state you can't place = a design question, not a new component
   :reachability  {:never "place state in a substrate domain so another layer can read it"}  ;; the entity IS the reachable home; pure UI selection state stays in Presentation
   :ui            :projection                                 ;; the UI SYSTEM reads the entity and raises the command pulses across the boundary; the view feeds it via a local C# event and never raises the pulse itself (PATTERN_VIEW_SYSTEM); owns zero transaction state (pure render state stays in the view)
   :command       "one-frame pulse, tiny identifying payload" ;; PATTERN_EVENT tolerance; consumed ONLY by a verb-domain reactive system that writes the entity
   :lifecycle     "stage = a …StateComponent COLUMN"          ;; one archetype for the whole session (Birth Completeness); the identity tag never changes. A stage that genuinely needs a DIFFERENT composition = delete the row + create a new one in its archetype, PK/FK carried over — never an optional column. Draft stage OPTIONAL (see Degenerate case)
   :completion    "FACT entity in the substrate domain + payload-less pulse"  ;; the transaction produces a noun; the pulse only says "re-read"
   :views         "render facts (or the live entity) — never a snapshot copy"
   :flow-contract "Flows/FLOW_<NAME>.md"})                    ;; REQUIRED per flow — events, ownership, ordering invariants, gap list; code comments link, never retell
```

## Anti-patterns

| Wrong | Why | Right |
|---|---|---|
| Transaction state as a world component in a substrate domain "so the other side can read it" | Substrate vocabulary polluted with session state; ownership inverts (Presentation writes Economy) | The state lives on the transaction entity; whoever needs it reads the entity |
| Snapshot payload across the boundary (a full state copy on the opening event) | A copy-at-a-moment goes stale; a re-stamp elsewhere papers over it | Identifying payload only; the consumer reads live state off the entity |
| A view renders the verb entity as if it were the result | When the verb gains stages (ticking), every consumer breaks semantically | Completion writes a fact; views reconcile from the fact table |
| One logical state duplicated per subdomain, synced by events | The copies drift; the invariants end up as prose in N docs | ONE entity, N readers |
