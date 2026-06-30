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

- **Base set = the work set** (the table the system processes — Table Rule: key + discriminator) **or a tick
  anchor** (a singleton whose presence gates the tick).
- **`LateUpdatedSystem`** when the system must observe the frame's final state (after camera / gameplay writes).
- **Stateless** — no cross-frame instance fields. Where state belongs instead: persistent → a component / world
  component; valid-only-this-frame → the `PreUpdate` + `FrameBox<T>` pattern; genuinely unavoidable →
  `[StateAllowed("reason")]`, a reviewed exception.
- **Per-frame shared inputs:** override `PreUpdate(GameState)` to resolve + fail-loud-guard everything the
  per-entity loop shares, and carry it to `Update` in a **`FrameBox<T>`** (`Core`) — frame-stamped (stale reads
  throw; `Dispose` drops held references); the field carries `[StateAllowed("...")]`. **Forbidden in UniTask
  systems** — an `await` spans frames, so the box goes stale.
- A per-frame system that scans/diffs collections each tick to detect change is a god-system smell — that IS
  reactive: emit a pulse where the change happens (`../ARCHITECTURE.md` → Decomposition).
- **Push to a View one value at a time** (ResourceBar pattern) — never build a managed snapshot inside a system.
- **Wiring:** register the **concrete** type in the installer, then wire it into a game state by hand in
  `Boot.Construct` (almost always `Gameplay`). A system not wired into a state never runs.
```
