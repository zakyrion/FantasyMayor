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
domain logic of its own. It is the composition of two existing recipes — reactive **trigger** (driven by the
event's archetype) + orchestrator **fan-out** (subsystems do the work). Use it when a single event must drive several
independent mechanics (e.g. confirm-build → spawn entity, spend resources, set turns-left).

```clojure
(cond
  (handling-fits-one-Update-body?) PATTERN_REACTIVE_SYSTEM
  :else                            reactive-orchestrator)  ;; complex / several independent parts / unsure it belongs in one file
```

## Orchestrator (reactive: the event's archetype drives it)

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem
{
    private readonly EntityStorages _storages;

    // DI-collected subsystems. Ordered once; fixed composition, not per-frame state — hence [StateAllowed].
    [StateAllowed("DI-collected composition, fixed at construction")]
    private readonly IReadOnlyList<[Name]SubSystem> _subSystems;

    public override int Priority => SystemPriorities.RuntimeTick.[Name];

    // DI injects EntityStorages — never a bare EntityStore.
    public [Name]System(EntityStorages storages, IReadOnlyList<[Name]SubSystem> subSystems)
        : base(storages.World, EventArchetypes.Of<[Name]Event>(storages.World))   // the pulse's archetype drives the system
    {
        _storages = storages;
        _subSystems = subSystems.OrderBy(s => s.Priority).ToArray();
    }

    // Subsystems act on the event's values or reconcile against CURRENT store state.
    protected override void Update(GameState state, in Entity pulse)
    {
        if (!EcsEventExtensions.IsRipe(pulse))
            return;

        for (var i = 0; i < _subSystems.Count; i++)
            if (_subSystems[i].IsEnabled)
                _subSystems[i].Run(/* shared args, the event's values */);
    }
}
```

No role marker here: the event archetype sits in the `base(...)` call, so the shape decides the role and a
marker is forbidden. An orchestrator that instead implements `IUpdatedSystem` directly and HOLDS the event
archetype in a field is the one shape no ordering rule decides, so it carries the marker:

```csharp
[UsedImplicitly]
[SystemRole(SystemRoleKind.Reactive)]   // Update-loop class holding an event archetype outside base(...)
public sealed class [Name]System : IUpdatedSystem
```

`Reactive` demands the Update-loop contract with no table anchor plus an event anchor or a held event
archetype — a marker that contradicts the class shape FAILS the Unity compile (MarkerShapeAnalyzer,
category `FantasyMayor.Markers`, severity Error). No marker is inherited: every concrete class carries
its own, and the subsystems — plain objects, not systems — carry none.

The subsystem base + concretes are exactly [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md) —
a plain `IDisposable` object (NOT a system). Query caches belong to the store, so a subsystem has nothing to
release there: its `Dispose` clears only what it allocated itself (ARCHITECTURE → Runtime forms, :orchestrator/query-caches).

## Incremental start — the reactive shell

VContainer throws on an **empty** `IReadOnlyList<T>` collection injection. So when you stand the orchestrator up
before its first subsystem exists, ship it as a **reactive shell**: driven by the event's archetype, empty `Update`, and
do **not** inject the subsystem list yet. Add the `IReadOnlyList<[Name]SubSystem>` field + the fan-out loop with
the first concrete subsystem. Live shell instance: `BuildDistrictActionSystem`
(`Assets/Domains/Actions/BuildDistrictAction/Systems/`) — reacts to `DistrictBuildConfirmedEvent`, no subsystems
yet.

## Rules

```clojure
(def reactive-orchestrator-rules
  {:driven-by       "EventArchetypes.Of<TheEvent>"                ;; zero cost while no pulse exists
   :role-marker     {:required  "[SystemRole(SystemRoleKind.Reactive)] when the orchestrator HOLDS the event archetype outside base(...)"
                     :forbidden "on the base-anchored shape above, and on every subsystem — a subsystem is not a system"
                     :inherited :never}                           ;; a marker contradicting the shape is a compile ERROR (MarkerShapeAnalyzer); a redundant one is a graph warning today
   :on-pulse        #{"subsystems act on the event's values"
                      "subsystems reconcile against CURRENT state"} ;; a reconciling Run() rebuilds and diffs — idempotent
   :ripe-gate       "IsRipe(pulse) or return"                     ;; the orchestrator gates ONCE, before the fan-out — a subsystem never re-checks (ARCHITECTURE → Events)
   :orchestrator    {:contains :no-domain-logic}                  ;; sort by Priority, skip IsEnabled==false, Run — ALL real work is in the subsystems
   :subsystem       "plain IDisposable, NOT a system"             ;; [StateAllowed("reason")] on the orchestrator's list; query caches live in each subsystem
   :di-subsystem    ".As<[Feature]SubSystem, [Name]SubSystem>()"  ;; register AS the base so VContainer fills the list
   :di-orchestrator "concrete + wired in Boot.Construct"          ;; per-frame/reactive systems are grouped into a GameMode by hand
   :empty-collection :forbidden                                   ;; no subsystems yet → reactive shell (no list injection) until the first one lands
   :structural-in-update :safe                                    ;; UpdatedSystem snapshots the driving archetype's ids and calls Update(state, entity) outside its enumeration
   :structural-in-own-enumeration :forbidden})                    ;; an enumeration a subsystem opens itself: snapshot entity.Id into NativeList<int>, re-fetch via TryGetEntityById, then delete/modify (ARCHITECTURE → Threading and structural change)
```
