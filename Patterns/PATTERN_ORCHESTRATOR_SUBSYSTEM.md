---
category: B
read: trigger
trigger: "before creating an orchestrator + subsystem family (DoD polymorphism / independently ordered parts)"
tags: [pattern, ecs, systems, di]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_PIPELINE_STAGE](PATTERN_PIPELINE_STAGE.md)"
---

# Pattern — Orchestrator + SubSystems

A family of implementations behind one abstract base, DI-collected into an orchestrator that sequences them by
Priority. Use for one-base-one-impl-per-feature (DoD polymorphism) or a stage with several independently
ordered parts. One approach.

## Abstract subsystem base (a plain object, NOT a system)

```csharp
public abstract class [Name]SubSystem : IDisposable
{
    protected readonly World World;
    protected [Name]SubSystem(World world) => World = world;

    public bool IsEnabled { get; set; } = true;
    public abstract int Priority { get; }
    public abstract void Run([Args]);     // the family's one operation (e.g. Spawn / Populate / TrySpawn)
    public virtual void Dispose() { }
}
```

## Concrete subsystem

```csharp
[UsedImplicitly]
public sealed class [Feature]SubSystem : [Name]SubSystem
{
    private const int ExecutionPriority = [N];
    public override int Priority => ExecutionPriority;

    public [Feature]SubSystem(World world) : base(world) { }

    public override void Run([Args])
    {
        // This feature's work; build its own query caches in the ctor, dispose them in Dispose.
    }
}
```

## Orchestrator (a pipeline stage OR a per-frame system)

```csharp
[StateAllowed]   // the host IS a system; the list is fixed composition, not per-frame state
private readonly IReadOnlyList<[Name]SubSystem> _subSystems;

public [Name]System(IReadOnlyList<[Name]SubSystem> subSystems /*, World world ... */)
{
    _subSystems = subSystems.OrderBy(s => s.Priority).ToArray();
}

// In Update: foreach subsystem in order → if (subSystem.IsEnabled) subSystem.Run(...);
```

## Rules

- **The orchestrator holds NO domain logic** — sort by Priority, skip `IsEnabled == false`, run. All real work
  is in the subsystems.
- **DI collection:** register each concrete subsystem AS the base type —
  `.As<[Feature]SubSystem, [Name]SubSystem>()` — so VContainer fills the orchestrator's
  `IReadOnlyList<[Name]SubSystem>`. The orchestrator registers by its host contract (pipeline stage →
  `IPrioritizedUniTaskSystem<MapGenerationStep>`; per-frame → concrete, then wired in `Boot.Construct`).
- **`[StateAllowed]` on the list applies only when the orchestrator is itself a system** (the two arch-check
  bans target systems): the `IReadOnlyList` + `OrderBy().ToArray()` are fixed composition. The subsystem base
  is a plain `IDisposable` (NOT a system) — arch-check ignores it, so it may freely hold state and query caches.
- **The base owns SHARED queries / `Try…` helpers;** each subsystem owns and disposes its OWN extra queries.
- Subsystem priorities order children WITHIN the orchestrator only — unrelated to pipeline-stage priorities.
- **Routing variant (DoD polymorphism):** make the operation return `bool` (`TrySpawn(config)`); the
  orchestrator tries each subsystem until one handles the input, and **fails loud** when none does.
```
