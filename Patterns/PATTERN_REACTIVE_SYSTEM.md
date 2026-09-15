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

**The default for runtime logic.** Responds to a one-frame [event](PATTERN_EVENT.md) through its archetype.
On the event it acts on the event's values directly or reconciles against current store data. Subclass
`UpdatedSystem`. One approach.

## Skeleton

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem
{
    private readonly EntityStorages _storages;
    private readonly ComponentIndex<[Key]Component, [KeyValue]> _rowsByKey;  // declarative cache (Table Rule)
    private readonly Archetype _rows;                                        // the table this system reconciles

    public override int Priority => SystemPriorities.RuntimeTick.[Name];

    // DI injects EntityStorages — never a bare EntityStore.
    public [Name]System(EntityStorages storages)
        : base(storages.World, EventArchetypes.Of<[Name]Event>(storages.World))   // the pulse's archetype drives the system
    {
        _storages = storages;
        _rowsByKey = storages.World.ComponentIndex<[Key]Component, [KeyValue]>();
        _rows = [Domain]Archetypes.[Table](storages.World);
    }

    // Act on the event's values directly, or reconcile against current store state.
    protected override void Update(GameState state, in Entity pulse)
    {
        if (!EcsEventExtensions.IsRipe(pulse))   // deliver exactly once, the frame after the pulse was raised
            return;

        // 1. Guard prerequisites (fail loud on the critical ones).
        // 2. Read the event's values — pulse.GetComponent<[Name]Event>() — or build the CURRENT desired state from store data.
        // 3. Act — when reconciling, only on the difference against what exists (idempotent).
    }
}
```

## Rules

```clojure
(def reactive-rules
  {:on-pulse #{"act on the event's values directly"
               "reconcile: rebuild desired state from CURRENT store data and diff"}  ;; reconcile is idempotent: a 2nd pulse finds nothing to do; a missed pulse is repaired by the next
   :driven-by "EventArchetypes.Of<TheEvent>" ;; zero cost while no pulse exists
   :ripe-gate "IsRipe(pulse) or return"      ;; MANDATORY first line: without it the system reacts on the birth frame too, i.e. twice (ARCHITECTURE → Events)
   :priority  "deterministic order only"     ;; delivery no longer depends on it — never encode "must sit above/below the producer"
   :structural-in-update :safe               ;; UpdatedSystem snapshots the driving archetype's ids and calls Update(state, entity) outside its enumeration
   :structural-in-own-enumeration :forbidden ;; an enumeration the system opens inside Update: snapshot entity.Id into NativeList<int>, re-fetch via TryGetEntityById, then delete/modify (ARCHITECTURE → Threading, structural-change)
   :smell    {:create+destroy-same-content "two reactive systems, one event each"
              :per-frame-just-to-check     "it IS reactive"}  ;; emit the pulse at the change source
   :wiring   "concrete in installer + wired in Boot.Construct"})
```
