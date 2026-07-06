---
category: B
read: trigger
trigger: "before creating a reactive system whose event handling has several independently-ordered parts (fan-out)"
tags: [pattern, ecs, systems, reactive, di]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
---

# Pattern — Reactive Orchestrator System (pulse → fan-out)

A [reactive system](PATTERN_REACTIVE_SYSTEM.md) whose handling is **too big for one file**: on the pulse it fans
out to a DI-collected [subsystem family](PATTERN_ORCHESTRATOR_SUBSYSTEM.md) ordered by `Priority`, and owns no
domain logic of its own. It is the composition of two existing recipes — reactive **trigger** (base set is the
event) + orchestrator **fan-out** (subsystems do the work). Use it when a single event must drive several
independent mechanics (e.g. confirm-build → spawn entity, spend resources, set turns-left).

```lisp
(reactive-system      → handling fits one Update body)                 ;; use PATTERN_REACTIVE_SYSTEM
(reactive-orchestrator → handling is complex / several independent parts / unsure it belongs in one file)
```

## Orchestrator (reactive: base set IS the event)

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem
{
    // Reactive: must run before the cleanup pass so the pulse is consumed the tick it is raised.
    private const int ExecutionPriority = [N];

    private readonly World _world;

    // DI-collected subsystems. Ordered once; fixed composition, not per-frame state — hence [StateAllowed].
    [StateAllowed]
    private readonly IReadOnlyList<[Name]SubSystem> _subSystems;

    public override int Priority => ExecutionPriority;

    public [Name]System(World world, IReadOnlyList<[Name]SubSystem> subSystems)
        : base(world.GetEntities().With<[Name]Event>().AsSet())   // the pulse IS the base set
    {
        _world = world;
        _subSystems = subSystems.OrderBy(s => s.Priority).ToArray();
    }

    // The pulse entity is ignored — each subsystem reconciles against CURRENT world state (idempotent).
    protected override void Update(GameState state, in Entity pulse)
    {
        for (var i = 0; i < _subSystems.Count; i++)
            if (_subSystems[i].IsEnabled)
                _subSystems[i].Run(/* shared args */);
    }
}
```

The subsystem base + concretes are exactly [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md) —
a plain `IDisposable` object (NOT a system), each owning and disposing its own query caches.

## Incremental start — the reactive shell

VContainer throws on an **empty** `IReadOnlyList<T>` collection injection. So when you stand the orchestrator up
before its first subsystem exists, ship it as a **reactive shell**: base set on the event, empty `Update`, and
do **not** inject the subsystem list yet. Add the `IReadOnlyList<[Name]SubSystem>` field + the fan-out loop with
the first concrete subsystem. Live shell instance: `BuildDistrictActionSystem`
(`Assets/Domains/Actions/BuildDistrictAction/Systems/`) — reacts to `DistrictBuildConfirmedEvent`, no subsystems
yet.

## Rules

```lisp
(base-set          → With<TheEvent>)                        ;; zero cost while no pulse exists; the event is payload-less (PATTERN_EVENT) — persistent truth lives in world components / on entities
(on-pulse          → subsystems reconcile, never delta)     ;; each Run() rebuilds from CURRENT state and diffs — idempotent
(priority          < cleanup-pass)                          ;; so the pulse is consumed the tick it is raised
(orchestrator      :contains no-domain-logic)               ;; sort by Priority, skip IsEnabled==false, Run — ALL real work is in the subsystems
(subsystem         → plain IDisposable, NOT a system)       ;; [StateAllowed] on the orchestrator's list; query caches live in each subsystem
(di :subsystem     → .As<[Feature]SubSystem, [Name]SubSystem>())  ;; register AS the base so VContainer fills the list
(di :orchestrator  → concrete + wired in Boot.Construct)    ;; per-frame/reactive systems are grouped into a GameMode by hand
(empty-collection  → forbidden)                             ;; no subsystems yet → reactive shell (no list injection) until the first one lands
(destroy-while-iterating → forbidden)                       ;; snapshot into NativeList<Entity> first, then destroy
```
