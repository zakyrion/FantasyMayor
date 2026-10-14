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

## Orchestrator (reactive: its own reader drives it)

```csharp
[UsedImplicitly]
public sealed class [Name]System : IUpdatedSystem
{
    private readonly EntityStorages _storages;
    private readonly EventReader<[Name]Event> _<reader>;

    // DI-collected subsystems, selected and ordered once; fixed composition, not per-frame state — hence [StateAllowed].
    [StateAllowed("DI-collected composition, fixed at construction")]
    private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

    public AppState AppState { get; }
    public int Priority => SystemPriorities.RuntimeTick.[Name];

    // DI injects EntityStorages and builds a fresh EventReader<TEvent> — never a bare EntityStore.
    public [Name]System(AppState appState, EntityStorages storages, EventReader<[Name]Event> <reader>,
        IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
    {
        AppState = appState;
        _storages = storages;
        _<reader> = <reader>;
        _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof([Name]System), allSubSystems);
    }

    // One fan-out per event — subsystems act on its values or reconcile against CURRENT store state.
    public void Update(GameState state)
    {
        while (_<reader>.TryRead(out var evt))
            RunSubSystems();
    }

    // The family is UniTask-based; a main-thread run completes synchronously while no part awaits. A pending run
    // means a part started waiting — this host then starts the run and checks its status on each Update instead.
    private void RunSubSystems()
    {
        var run = OrchestratorSubSystems.RunAsync(_subSystems, StatusMonitor.Token);
        if (run.Status == UniTaskStatus.Pending)
            throw new InvalidOperationException("[Name]System: a subsystem awaited — every part must complete synchronously.");
        run.GetAwaiter().GetResult();
    }
}
```

No role marker here: holding an `EventReader<TEvent>` in a class whose only base is the Update-loop
contract already decides `reactive`. `Reactive` has no legal marker value any more — the marker knows only
`PerFrame`, for the one shape where a class holds an `EventReader<TEvent>` yet still ticks every frame
instead of reacting. No marker is inherited: every concrete class carries its own, and the subsystems —
plain objects, not systems — carry none.

A subsystem that needs its own event holds its own `EventReader<TEvent>` — the orchestrator never
forwards one from its own reader (`orchestrator/own-reader`).

The subsystem base + concretes are exactly [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md) —
an `IPrioritizedUniTaskSystem` naming this orchestrator in `OrchestratorType`, a plain object (NOT a system).
A subsystem's event values reach it through its own reader, never as an argument. Query caches belong to the store, so a subsystem has nothing to
release there: its `Dispose` clears only what it allocated itself (ARCHITECTURE → Runtime forms, :orchestrator/query-caches).

## Incremental start — the reactive shell

`OrchestratorSubSystems.SelectForOrchestrator` throws when no subsystem names the orchestrator. So when you stand the
orchestrator up before its first subsystem exists, ship it as a **reactive shell**: driven by its own
`EventReader<TEvent>`, empty `Update`, and do **not** take the subsystem list yet. Add the
`IReadOnlyList<IPrioritizedUniTaskSystem>` parameter, the selection and the fan-out with the first concrete subsystem. Live shell instance: `BuildDistrictActionSystem`
(`Assets/Domains/Actions/BuildDistrictAction/Systems/`) — reacts to `DistrictBuildConfirmedEvent`, no subsystems
yet.

## Rules

```clojure
(def reactive-orchestrator-rules
  {:driven-by       "readonly EventReader<TEvent>, born Transient from DI"  ;; the cursor lives in EventLog, not on the orchestrator (event/reader)
   :role-marker     {:required  "[SystemRole(SystemRoleKind.PerFrame)] on an Update-loop class that holds an EventReader<TEvent> yet ticks every frame"
                     :forbidden "on a class holding no EventReader field, and on every subsystem — a subsystem is not a system"
                     :inherited :never}                           ;; a marker contradicting the shape is a compile ERROR (MarkerShapeAnalyzer); a redundant one is a graph warning today
   :on-pulse        #{"subsystems act on the event's values"
                      "subsystems reconcile against CURRENT state"} ;; a reconciling Run() rebuilds and diffs — idempotent
   :quiet-exit      "inside the fan-out, or after the reader is drained — never before"  ;; the reader must be given the chance to read first (event/drain-before-exit)
   :orchestrator    {:contains :no-domain-logic}                  ;; select by OrchestratorType, sort by Priority, skip IsEnabled==false, run — ALL real work is in the subsystems
   :subsystem       "IPrioritizedUniTaskSystem naming this orchestrator — a plain object, NOT a system"  ;; [StateAllowed("reason")] on the orchestrator's list; query caches live in each subsystem; a subsystem needing its own event holds its own EventReader<TEvent> (orchestrator/own-reader)
   :di-subsystem    "builder.Register<[Feature]SubSystem>(Lifetime.X).As<IPrioritizedUniTaskSystem>()"  ;; OrchestratorType routes it to this orchestrator
   :di-orchestrator "builder.RegisterAppStateSystem<[Name]System>(Lifetime.X, AppState.Gameplay)"  ;; the Gameplay state collects it; Boot names no system (ARCHITECTURE → :wiring/app-state-registration)
   :empty-family    :forbidden                                    ;; the selection throws on an empty family → reactive shell (no list) until the first subsystem lands
   :structural-in-update :safe                                    ;; a reader never enumerates entities, so structural changes inside the fan-out are safe
   :structural-in-own-enumeration :forbidden})                    ;; an enumeration a subsystem opens itself: snapshot entity.Id into NativeList<int>, re-fetch via TryGetEntityById, then delete/modify (ARCHITECTURE → Threading and structural change)
```
