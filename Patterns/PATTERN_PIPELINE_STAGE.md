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

    private readonly World _world;

    public int Priority => ExecutionPriority;

    public [Name]System(World world)
    {
        _world = world;
    }

    public async UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
    {
        if (!_world.Has<[Prerequisite]Component>())
            throw new InvalidOperationException("[Name]System: [Prerequisite]Component is missing.");

        if (cancellationToken.IsCancellationRequested)
            return;

        // Build content: create entities, world.Set runtime components, load prefabs.
    }

    public void Dispose()
    {
        // Release owned handles (addressables, views); dispose query caches.
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
   :family-of-parts     PATTERN_ORCHESTRATOR_SUBSYSTEM})                ;; several independently ordered parts / one-base-many-impls
```
