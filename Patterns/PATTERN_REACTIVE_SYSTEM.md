---
category: B
read: trigger
trigger: "before creating a reactive (event-driven) system"
tags: [pattern, ecs, systems, reactive]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
---

# Pattern — Reactive System (pulse + reconcile)

**The default for runtime logic.** Responds to a one-frame [event](PATTERN_EVENT.md): the event's archetype
drives the system; on the pulse, rebuild the desired state from current store data and diff. Subclass
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

    // DI injects EntityStorages — never a bare EntityStore (ECS_CONVENTIONS → State Storage Taxonomy).
    public [Name]System(EntityStorages storages)
        : base(storages.World, EventArchetypes.Of<[Name]Event>(storages.World))   // the pulse's archetype drives the system
    {
        _storages = storages;
        _rowsByKey = storages.World.ComponentIndex<[Key]Component, [KeyValue]>();
        _rows = [Domain]Archetypes.[Table](storages.World);
    }

    // The pulse entity carries no payload — reconciliation is global over current state.
    protected override void Update(GameState state, in Entity pulse)
    {
        if (!EcsEventExtensions.IsRipe(pulse))   // deliver exactly once, the frame after the pulse was raised
            return;

        // 1. Guard prerequisites (fail loud on the critical ones).
        // 2. Build the CURRENT desired state from store data.
        // 3. Diff against what exists; act only on the difference (idempotent).
    }
}
```

## Rules

```clojure
(def reactive-rules
  {:on-pulse "reconcile, never delta"        ;; rebuild desired state from CURRENT store data and diff — idempotent: a 2nd pulse finds nothing to do; a missed pulse is repaired by the next
   :driven-by "EventArchetypes.Of<TheEvent>" ;; zero cost while no pulse exists; the event is payload-less (PATTERN_EVENT) — persistent truth lives in a singleton component / on an entity
   :ripe-gate "IsRipe(pulse) or return"      ;; MANDATORY first line: without it the system reacts on the birth frame too, i.e. twice (ECS_CONVENTIONS → Event Lifecycle)
   :priority  "deterministic order only"     ;; delivery no longer depends on it — never encode "must sit above/below the producer"
   :structural-while-iterating :forbidden    ;; snapshot entity.Id into NativeList<int>, re-fetch via TryGetEntityById, then delete/modify (StructuralChangeException is store-wide)
   :smell    {:create+destroy-same-content "two reactive systems, one event each"
              :per-frame-just-to-check     "it IS reactive"}  ;; emit the pulse at the change source
   :wiring   "concrete in installer + wired in Boot.Construct"})
```
