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

```clojure
(def orchestrator-rules
  {:orchestrator    {:contains :no-domain-logic}                 ;; sort by Priority, skip IsEnabled==false, run — ALL real work is in the subsystems
   :di-subsystem    ".As<[Feature]SubSystem, [Name]SubSystem>()" ;; register AS the base so VContainer fills the orchestrator's IReadOnlyList<[Name]SubSystem>
   :di-orchestrator "its host contract"                          ;; pipeline stage → .As<IPrioritizedUniTaskSystem<MapGenerationStep>>; per-frame → concrete + wired in Boot.Construct
   :StateAllowed    {:only-when "orchestrator is a system"}      ;; the list + OrderBy().ToArray() are fixed composition; the subsystem base is plain IDisposable (NOT a system) — arch-check ignores it, state/query caches are free there
   :queries         {:shared "the base owns them + Try… helpers"
                     :own    "each subsystem owns AND disposes its own"}
   :priority-scope  "children WITHIN the orchestrator only"      ;; unrelated to pipeline-stage priorities
   :routing-variant "bool TrySpawn(config), first match wins, fail loud on none"  ;; DoD polymorphism
   :naming          "no domain prefix on [Name]/[Feature]"})     ;; namespace carries it; [Domain]Installer keeps its prefix (the documented exception; ECS_CONVENTIONS → Naming & Construction)
```
