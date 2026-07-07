---
category: B
read: trigger
trigger: "before creating a per-frame system (genuinely continuous logic)"
tags: [pattern, ecs, systems]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
---

# Pattern — Per-Frame System

Logic that is genuinely continuous: camera movement, per-frame projection, input polling, selection watching.
**Default to [reactive](PATTERN_REACTIVE_SYSTEM.md) instead** — use per-frame only when you can state WHY a
pulse cannot replace the tick. Subclass `UpdatedSystem` (or `LateUpdatedSystem`). One approach.

## Skeleton

```csharp
[UsedImplicitly]
public sealed class [Name]System : UpdatedSystem   // or LateUpdatedSystem
{
    private const int ExecutionPriority = [N];
    private readonly World _world;

    public override int Priority => ExecutionPriority;

    public [Name]System(World world)
        : base(world.GetEntities().With<[Anchor]Component>().AsSet())   // work set OR tick anchor
    {
        _world = world;
    }

    protected override void Update(GameState state, in Entity entity)
    {
        // Per-frame work. All inputs from world state — no cross-frame instance fields.
    }
}
```

## Rules

```clojure
(def per-frame-rules
  {:base-set            #{work-set tick-anchor}               ;; the table it processes (Table Rule: key + discriminator) OR a singleton whose presence gates the tick
   LateUpdatedSystem    {:when "observe final frame state"}   ;; after camera / gameplay writes
   :state               "none across frames"                  ;; persistent → component / world component; this-frame-only → PreUpdate + FrameBox<T>; genuinely unavoidable → [StateAllowed("reason")], a reviewed exception
   :shared-inputs       "PreUpdate(GameState) resolve + fail-loud, carry in FrameBox<T>" ;; frame-stamped: stale reads throw, Dispose drops refs; field carries [StateAllowed]; FORBIDDEN in UniTask systems — await spans frames, the box goes stale
   :scan-diff-each-tick "god-system smell — make it reactive" ;; emit a pulse where the change happens (ECS_CONVENTIONS → Decomposition)
   :view-output         "push ONE value at a time"            ;; ResourceBar pattern — never build a managed snapshot inside a system
   :wiring              "concrete in installer + hand-wired in Boot.Construct"})  ;; almost always Gameplay; a system not wired into a state never runs
```
