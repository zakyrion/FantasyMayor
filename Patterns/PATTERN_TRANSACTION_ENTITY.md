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

## Skeleton

The verb domain owns the entity and EVERY write to it. Stages are tags (Tag Law: the tag is the
table discriminator); state fields are ordinary components on the same entity.

```csharp
// 1. Open — a reactive system in the VERB domain spawns the transaction entity.
var draft = _world.CreateEntity();
draft.Set(new HexIdComponent { Coords = started.Coords });          // identifying state from the opening command
draft.Set(new DistrictTypeComponent { Value = DistrictType.None }); // session state, owned HERE from birth
draft.Set(new BuildDistrictActionTemplateTag());                    // stage tag: draft

// 2. Mutate — every change arrives as a COMMAND pulse (tiny identifying payload, PATTERN_EVENT)
//    consumed by a small reactive system in the verb domain; the UI never writes the entity.
protected override void Update(GameState state, in Entity pulse)
{
    var picked = pulse.Get<DistrictBuildSelectionRequestedEvent>().Type;
    _drafts.GetEntities()[0].Set(new DistrictTypeComponent { Value = picked });
}

// 3. Commit — promote by tag swap; the entity already carries the full, live transaction state.
entity.Remove<BuildDistrictActionTemplateTag>();
entity.Set(new BuildDistrictActionTag());

// 4. Complete — write the FACT into the substrate domain, then a payload-less pulse.
var fact = _world.CreateEntity();
fact.Set(new HexIdComponent { Coords = hex.Coords });
fact.Set(new DistrictTypeComponent { Value = type });
fact.Set(new DistrictTag());                                        // the substrate's own table

var pulse = _world.CreateEntity();
pulse.Set(new DistrictBuiltEvent());                                // consumers reconcile from the fact table
pulse.Set(new EventTag());
```

The UI side is a projection: its subsystems read the transaction entity directly
(Presentation → Domains is the allowed asmdef direction) and reconcile idempotently; user input
becomes command pulses, never writes.

## Rules

```clojure
(def transaction-entity-rules
  {:home          "verb domain (Actions)"                     ;; the verb owns its session — never the substrate, never Presentation
   :state         "ALL transaction state on the ONE entity"   ;; no world-component copies, no second home; state you can't place = a design question, not a new component
   :reachability  {:never "place state in a substrate domain so another layer can read it"}  ;; how DistrictBuildSelectionComponent ended up in Economy — the entity IS the reachable home
   :ui            :projection                                 ;; reads the entity, raises command pulses, owns zero transaction state (pure render state stays in the view)
   :command       "one-frame pulse, tiny identifying payload" ;; PATTERN_EVENT tolerance; consumed ONLY by a verb-domain reactive system that Set()s the entity
   :lifecycle     "stage = tag swap"                          ;; …TemplateTag → …ActionTag → …; filters stay tag-anchored (Tag Law)
   :completion    "FACT entity in the substrate domain + payload-less pulse"  ;; the transaction produces a noun; the pulse only says "re-read"
   :views         "render facts (or the live entity) — never a snapshot copy"
   :flow-contract "Flows/FLOW_<NAME>.md"})                    ;; REQUIRED per flow — events, ownership, ordering invariants, gap list; module MDs link, never retell
```

## Anti-patterns

| Wrong | Why | Right |
|---|---|---|
| Transaction state as a world component in a substrate domain "so the other side can read it" | Substrate vocabulary polluted with session state; ownership inverts (Presentation writes Economy) | The state lives on the transaction entity; whoever needs it reads the entity |
| Snapshot payload across the boundary (`DistrictBuildStartedEvent.Type`) | A copy-at-a-moment goes stale; a re-stamp elsewhere papers over it | Identifying payload only; the consumer reads live state off the entity |
| A view renders the verb entity as if it were the result | When the verb gains stages (ticking), every consumer breaks semantically | Completion writes a fact; views reconcile from the fact table |
| One logical state duplicated per subdomain, synced by events | The copies drift; the invariants end up as prose in N docs | ONE entity, N readers |
