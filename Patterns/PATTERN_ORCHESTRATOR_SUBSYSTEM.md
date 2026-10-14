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

A family of implementations behind one abstract base. Every subsystem implements `IPrioritizedUniTaskSystem` and
names its orchestrator in `OrchestratorType`; the orchestrator takes every subsystem from DI, keeps its own and runs
them by Priority. Use for one-base-one-impl-per-feature (DoD polymorphism) or a stage with several independently
ordered parts. A subsystem is never a system: no AppState, never `IUniTaskSystem`, never `IAppStateSystem`. One
approach.

## Abstract subsystem base (a plain object, NOT a system)

```csharp
public abstract class [Name]SubSystem : IPrioritizedUniTaskSystem, IDisposable
{
    // The base takes the bare store — it is not DI-constructed; the concrete below unwraps EntityStorages.
    protected readonly EntityStore World;
    protected [Name]SubSystem(EntityStore world) => World = world;

    // The orchestrator keeps the subsystems whose OrchestratorType is its own type.
    public Type OrchestratorType => typeof([Name]System);

    public bool IsEnabled { get; set; } = true;
    public abstract int Priority { get; }
    public abstract UniTask Update(CancellationToken cancellationToken);   // the family's one operation
    public virtual void Dispose() { }
}
```

## Concrete subsystem

```csharp
[UsedImplicitly]
public sealed class [Feature]SubSystem : [Name]SubSystem
{
    // SystemPriorities is the one holder of execution order — never a class-local const, never a literal.
    // Subsystem values live in their own family space and compare only inside their own orchestrator.
    public override int Priority => SystemPriorities.SubSystems.[Name].[Feature];

    // DI injects EntityStorages — never a bare EntityStore.
    public [Feature]SubSystem(EntityStorages storages) : base(storages.World) { }

    public override UniTask Update(CancellationToken cancellationToken)
    {
        // This feature's work; resolve its own archetypes / indices once in the ctor.
        return UniTask.CompletedTask;
    }
}
```

## Orchestrator (a pipeline stage OR an Update host)

```csharp
[StateAllowed("DI-collected composition, fixed at construction")]   // the host IS a system; the marker always carries its reason
private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

public [Name]System(AppState appState, EntityStorages storages, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
{
    AppState = appState;
    // keeps the ones naming this orchestrator, ordered by Priority; throws when none names it
    _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof([Name]System), allSubSystems);
}

// A stage: in Execute → await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
// runs the enabled ones in order, ThrowIfCancellationRequested between them.
```

## Rules

```clojure
(def orchestrator-rules
  {:orchestrator    {:contains :no-domain-logic}                 ;; select by OrchestratorType, sort by Priority, skip IsEnabled==false, run — ALL real work is in the subsystems
   :selection       "OrchestratorSubSystems.SelectForOrchestrator(typeof([Name]System), allSubSystems)"  ;; once, in the ctor; throws when no subsystem names the orchestrator
   :run             "OrchestratorSubSystems.RunAsync(_subSystems, token)"  ;; enabled ones only, in order
   :di-subsystem    "builder.Register<[Feature]SubSystem>(Lifetime.X).As<IPrioritizedUniTaskSystem>()"  ;; one contract for every family; OrchestratorType, not the registration, routes it
   :di-orchestrator "builder.RegisterAppStateSystem<[Name]System>(Lifetime.X, AppState.<its state>)"  ;; a first-order system — its state collects it (ARCHITECTURE → :wiring/app-state-registration)
   :StateAllowed    {:only-when "orchestrator is a system"}      ;; the selected list is fixed composition; the subsystem base is a plain object (NOT a system) — arch-check ignores it, state/query caches are free there
   :queries         {:shared "the base owns them + Try… helpers"
                     :own    "each subsystem resolves its own archetypes / indices in its ctor"}  ;; store-owned, nothing to dispose
   :priority-scope  "children WITHIN the orchestrator only"      ;; unrelated to pipeline-stage priorities
   :by-kind-variant "every subsystem walks all entries and handles only its own kind; the orchestrator checks afterwards that every entry was handled, and throws on one that was not"  ;; DoD polymorphism — no routing, no first-match
   :naming          "no domain prefix on [Name]/[Feature]"})     ;; namespace carries it; [Domain]Installer keeps its prefix (the documented exception; ARCHITECTURE → Naming)
```
