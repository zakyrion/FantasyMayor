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

- **Reconcile, do not delta.** On the pulse, rebuild the desired set from current state and diff — never treat
  the event as a per-entity change. This makes the system **idempotent**: a second pulse the same frame is a
  no-op; a coalesced/missed pulse is repaired by the next.
- **Base set = `With<TheEvent>`** → the system costs nothing while no pulse exists. The event is payload-less
  (see [PATTERN_EVENT](PATTERN_EVENT.md)); persistent truth lives in a world component / on an entity — the
  event only says "re-read it".
- **Priority below the cleanup pass** so the pulse is consumed the tick it is raised.
- **Don't destroy entities while iterating** the map/set that indexes them — snapshot into a `NativeList<Entity>`
  first, then destroy.
- **Split smells:** one system that both creates and destroys the same content → two reactive systems, one
  event each. A per-frame "just to check" tick → it is reactive; emit a pulse at the change source.
- **Wiring:** register the concrete type, then wire it into a game state in `Boot.Construct`.
```
