---
category: B
read: trigger
trigger: "before creating a world-init pipeline stage (build/spawn content once during map creation)"
tags: [pattern, ecs, systems, pipeline]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
---

# Pattern — Pipeline Stage (one-shot, world-init)

One-shot async construction during map creation: spawn entities/views, build runtime singleton components, load
prefabs. Runs once, ordered against other stages by Priority. Implement
`IPrioritizedUniTaskSystem<MapGenerationStep>`. One approach.

## Skeleton

```csharp
[UsedImplicitly]
internal sealed class [Name]System : IPrioritizedUniTaskSystem<MapGenerationStep>
{
    private readonly EntityStorages _storages;
    private readonly EntityStore _world;
    private readonly Archetype _rows;   // the table this stage populates

    // SystemPriorities is the one holder of execution order — never a class-local const, never a literal.
    // Stages live in the WorldInit space: currently ~100..900, spaced by ~100.
    public int Priority => SystemPriorities.WorldInit.[Name];

    public [Name]System(EntityStorages storages)
    {
        _storages = storages;
        _world = storages.World;
        _rows = [Domain]Archetypes.[Table](_world);
    }

    public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
    {
        if (!_storages.Singletons.Has<[Prerequisite]Component>())
            throw new InvalidOperationException("[Name]System: [Prerequisite]Component is missing.");

        cancellationToken.ThrowIfCancellationRequested();

        // Build content: create rows via _rows.CreateEntity(), publish runtime singleton components, load prefabs.
    }

    public void Dispose()
    {
        // Release owned handles (addressables, views).
    }
}
```

## Rules

```clojure
(def pipeline-stage-rules
  {:execution           "sequential, ascending Priority, awaited"       ;; a stage may rely on everything lower-priority stages produced — fail loud on a missing prerequisite
   :re-entry            #{"_isLoaded guard — the field is state and carries [StateAllowed(«reason»)]" "destroy-and-recreate"}  ;; only if regeneration can re-enter the stage (ARCHITECTURE → System state, :state/lifetime-flags)
   :addressable-handle  "keep owned, release in Dispose"                ;; ADDRESSABLE_PATTERNS.md
   :cancellation        "cancellationToken.ThrowIfCancellationRequested()" ;; after every await; own line, never combined with a validity check; never a quiet return
   :singleton-view      "publish a …ViewComponent for consumers"
   :singleton-non-queried :singleton-component
   :wiring              ".As<IPrioritizedUniTaskSystem<MapGenerationStep>>" ;; pipeline auto-collects — no Boot.Construct edit
   :native-scratch      {:allocator "by lifetime: Allocator.TempJob within 4 frames, else Allocator.Persistent + one owner's Dispose"
                         :never "Allocator.Temp — a stage is an async system"}  ;; ARCHITECTURE → Collections and memory
   :thread-hops         {:off-thread "computation over plain data ONLY"       ;; code on RunOnThreadPool touches NO store call at all — not even a read — and NO Allocator.Temp
                         :store-access "only behind await UniTask.SwitchToMainThread()"}  ;; store-thread law (ARCHITECTURE → Threading and structural change)
   :family-of-parts     PATTERN_ORCHESTRATOR_SUBSYSTEM})                ;; several independently ordered parts / one-base-many-impls
```
