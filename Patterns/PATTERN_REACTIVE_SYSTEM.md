---
category: B
read: trigger
trigger: "before creating a reactive (event-driven) system"
tags: [pattern, ecs, systems, reactive]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
---

# Pattern — Reactive System (event-driven)

**The default for runtime logic.** Responds to a [log event](PATTERN_EVENT.md) it reads through its own
`EventReader<TEvent>`. On each event it acts on the event's values directly or reconciles against current
store data. Sealed `IUpdatedSystem` directly — no base at all. One approach.

## Skeleton

```csharp
[UsedImplicitly]
public sealed class [Name]System : IUpdatedSystem
{
    private readonly EntityStorages _storages;
    private readonly EventReader<[Name]Event> _<reader>;
    private readonly ComponentIndex<[Key]Component, [KeyValue]> _rowsByKey;  // declarative cache (Table Rule)
    private readonly Archetype _rows;                                        // the table this system reconciles

    public int Priority => SystemPriorities.RuntimeTick.[Name];

    // DI injects EntityStorages and builds a fresh EventReader<TEvent> — never a bare EntityStore.
    public [Name]System(EntityStorages storages, EventReader<[Name]Event> <reader>)
    {
        _storages = storages;
        _<reader> = <reader>;
        _rowsByKey = storages.World.ComponentIndex<[Key]Component, [KeyValue]>();
        _rows = [Domain]Archetypes.[Table](storages.World);
    }

    // One reaction per event — the reader is drained to the end every tick.
    public void Update(GameState state)
    {
        while (_<reader>.TryRead(out var evt))
            React(evt);
    }

    private void React(in [Name]Event evt)
    {
        // 1. Guard prerequisites (fail loud on the critical ones).
        // 2. Read the event's values from evt, or build the CURRENT desired state from store data.
        // 3. Act — when reconciling, only on the difference against what exists (idempotent).
    }
}
```

A reaction that instead fires ONCE per tick, on a whole drained batch rather than per event (e.g. a
turn-boundary reconcile), reads:

```csharp
public void Update(GameState state)
{
    if (!_<reader>.DrainBatch())   // false — nothing arrived this tick
        return;

    // reconcile once against current store state
}
```

No role marker on either shape above: holding an `EventReader<TEvent>` in a class whose only base is the
Update-loop contract already decides `reactive`. The marker `PerFrame` is required in exactly ONE shape —
an Update-loop class that ALSO holds an `EventReader<TEvent>` yet still ticks every frame rather than
reacting (PATTERN_PERFRAME_SYSTEM); `Reactive` has no legal marker value any more.

No marker is inherited: every concrete class carries its own. Live instance: `TurnCountSystem`.

## Rules

```clojure
(def reactive-rules
  {:on-pulse #{"act on the event's values directly"
               "reconcile: rebuild desired state from CURRENT store data and diff"}  ;; reconcile is idempotent: a 2nd event finds nothing to do; a missed event is repaired by the next
   :driven-by "readonly EventReader<TEvent>, born Transient from DI" ;; the cursor lives in EventLog, not on the system (event/reader)
   :role-marker {:required  "[SystemRole(SystemRoleKind.PerFrame)] on an Update-loop class that holds an EventReader<TEvent> yet ticks every frame"
                 :forbidden "on a class holding no EventReader field — reactive has no marker of its own"
                 :inherited :never}          ;; a marker contradicting the shape is a compile ERROR (MarkerShapeAnalyzer); a redundant one is a graph warning today
   :quiet-exit "inside the reaction, or after the reader is drained — never before" ;; the reader must be given the chance to read first (event/drain-before-exit)
   :priority  "deterministic order only"     ;; a reader after its producer's priority sees the event the same tick, before it the next tick — never encode "must sit above/below the producer" (event/delivery)
   :structural-in-update :safe               ;; a reader never enumerates entities, so structural changes inside the reaction are safe the same way they are inside an anchored Update body
   :structural-in-own-enumeration :forbidden ;; an enumeration the system opens inside Update: snapshot entity.Id into NativeList<int>, re-fetch via TryGetEntityById, then delete/modify (ARCHITECTURE → Threading and structural change)
   :smell    {:create+destroy-same-content "two reactive systems, one event each"
              :per-frame-just-to-check     "it IS reactive"}  ;; emit the event at the change source
   :wiring   "concrete in installer + wired in Boot.Construct"})
```
