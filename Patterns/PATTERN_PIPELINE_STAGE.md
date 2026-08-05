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

One-shot async construction during map creation: spawn entities/views, build runtime world components, load
prefabs. Runs once, ordered against other stages by Priority. Implement
`IPrioritizedUniTaskSystem<MapGenerationStep>`. One approach.

## Skeleton

```csharp
[UsedImplicitly]
internal sealed class [Name]System : IPrioritizedUniTaskSystem<MapGenerationStep>
{
    private const int ExecutionPriority = [N];   // current stages: ~100..900, spaced by ~100

    private readonly EntityStore _world;
    private readonly Archetype _rows;   // the table this stage populates

    public int Priority => ExecutionPriority;

    public [Name]System(EntityStore world)
    {
        _world = world;
        _rows = [Domain]Archetypes.[Table](world);
    }

    public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
    {
        if (!_world.HasWorldComponent<[Prerequisite]Component>())
            throw new InvalidOperationException("[Name]System: [Prerequisite]Component is missing.");

        if (cancellationToken.IsCancellationRequested)
            return;

        // Build content: create rows via _rows.CreateEntity(), publish runtime world components, load prefabs.
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
   :re-entry            #{"_isLoaded guard" "destroy-and-recreate"}     ;; only if regeneration can re-enter the stage
   :addressable-handle  "keep owned, release in Dispose"                ;; ADDRESSABLE_PATTERNS.md
   :quiet-return        "cancellationToken.IsCancellationRequested ONLY" ;; own line, never combined with a validity check
   :singleton-view      "publish a …ViewComponent for consumers"
   :singleton-non-queried :world-component
   :wiring              ".As<IPrioritizedUniTaskSystem<MapGenerationStep>>" ;; pipeline auto-collects — no Boot.Construct edit
   :native-scratch      {:never "Allocator.Temp across an await"}       ;; Temp is a per-thread stack rewound under you at frame/job end — scratch that spans an await = Allocator.Persistent + explicit Dispose (law: ECS_CONVENTIONS → Threading And Native Memory)
   :thread-hops         {:off-thread "computation over plain data ONLY"       ;; code on RunOnThreadPool touches NO store call at all — not even a read — and NO Allocator.Temp (its TLS block never rewinds there)
                         :store-access "only behind await UniTask.SwitchToMainThread()"}  ;; Law 1 (ECS_CONVENTIONS → Threading And Native Memory)
   :family-of-parts     PATTERN_ORCHESTRATOR_SUBSYSTEM})                ;; several independently ordered parts / one-base-many-impls
```
