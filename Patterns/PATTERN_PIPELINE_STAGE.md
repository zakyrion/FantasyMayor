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

- **Stages run sequentially in ascending Priority**, awaited by map creation. A stage may rely on everything
  lower-priority stages produced — fail loud if a prerequisite is missing.
- **Idempotency:** if the stage can be re-entered (regeneration), guard with an `_isLoaded` flag or
  destroy-and-recreate. Stages owning an addressable handle keep it and release in `Dispose`
  (`Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md`).
- `cancellationToken.IsCancellationRequested` is the only quiet `return` — own line, never combined with a
  validity check.
- A stage that creates a singleton **view** entity publishes it for consumers (a `...ViewComponent`); a stage
  that creates single-instance non-queried state uses a **world component**.
- **Wiring:** register as `IPrioritizedUniTaskSystem<MapGenerationStep>`; the pipeline auto-collects it — no
  `Boot.Construct` edit. Several independently ordered parts / a family of impls? →
  [PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md).
```
