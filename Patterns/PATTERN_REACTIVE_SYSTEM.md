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

**The default for runtime logic.** Responds to a one-frame [event](PATTERN_EVENT.md): the event is the base
set; on the pulse, rebuild the desired state from current world data and diff. Subclass `UpdatedSystem`. One
approach.

## Skeleton

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem
{
    // Must run BEFORE the cleanup pass so the pulse is consumed the same tick it is raised.
    private const int ExecutionPriority = [N];

    private readonly World _world;
    private readonly EntityMultiMap<[Key]Component> _rowsByKey;   // declarative query cache (Table Rule)

    public override int Priority => ExecutionPriority;

    public [Name]System(World world)
        : base(world.GetEntities().With<[Name]Event>().AsSet())   // the pulse IS the base set
    {
        _world = world;
        _rowsByKey = world.GetEntities()
            .With<[Key]Component>().With<[Discriminator]Tag>().AsMultiMap<[Key]Component>();
    }

    // The pulse entity is ignored — reconciliation is global over current state.
    protected override void Update(GameState state, in Entity pulse)
    {
        // 1. Guard prerequisites (fail loud on the critical ones).
        // 2. Build the CURRENT desired state from world data.
        // 3. Diff against what exists; act only on the difference (idempotent).
    }

    public override void Dispose()
    {
        _rowsByKey.Dispose();
        base.Dispose();
    }
}
```

## Rules

```lisp
(on-pulse          → reconcile, never delta)               ;; rebuild desired state from CURRENT world data and diff — idempotent: a 2nd pulse same frame = no-op; a missed pulse is repaired by the next
(base-set          → With<TheEvent>)                       ;; zero cost while no pulse exists; the event is payload-less (PATTERN_EVENT) — persistent truth lives in a world component / on an entity
(priority          < cleanup-pass)                         ;; so the pulse is consumed the tick it is raised
(destroy-while-iterating → forbidden)                      ;; snapshot into NativeList<Entity> first, then destroy
(smell :create+destroy-same-content → two reactive systems, one event each)
(smell :per-frame-just-to-check     → it IS reactive)      ;; emit the pulse at the change source
(wiring            → concrete in installer + wired in Boot.Construct)
```
