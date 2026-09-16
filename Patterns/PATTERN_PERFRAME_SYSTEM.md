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
    private readonly EntityStorages _storages;

    // SystemPriorities is the one holder of execution order — never a class-local const, never a literal.
    public override int Priority => SystemPriorities.RuntimeTick.[Name];

    // DI injects EntityStorages — never a bare EntityStore.
    public [Name]System(EntityStorages storages)
        : base(storages.World, [Domain]Archetypes.[Table](storages.World))   // work table OR tick anchor, resolved from its holder
    {
        _storages = storages;
    }

    protected override void Update(GameState state, in Entity entity)
    {
        // Per-frame work. All inputs from world state — no cross-frame instance fields.
    }
}
```

No role marker on the shape above: an Update-loop class with a table anchor in `base(...)` already decides
its own role, and a marker there is forbidden. The marker is required in exactly ONE shape — an Update-loop
class that ALSO holds an `EventReader<TEvent>` field yet still ticks every frame instead of reacting, where
nothing else can tell per-frame from reactive:

```csharp
[UsedImplicitly]
[SystemRole(SystemRoleKind.PerFrame)]        // ticks every frame; the held EventReader is only an input
public sealed class [Name]System : IUpdatedSystem
{
    private readonly EventReader<[Name]Event> _<reader>;   // held, drained every tick — never the sole driver

    public [Name]System(EntityStorages storages, EventReader<[Name]Event> <reader>)
    {
        _<reader> = <reader>;
    }
}
```

`PerFrame` demands the Update-loop contract PLUS an `EventReader<TEvent>` field — a marker that contradicts
the class shape FAILS the Unity compile (MarkerShapeAnalyzer, category `FantasyMayor.Markers`, severity
Error). No marker is inherited: every concrete class carries its own. Live instance: `TurnProcessorSystem`.

## Rules

```clojure
(def per-frame-rules
  {:driven-by           #{work-table tick-anchor}             ;; the declared archetype it processes (Table Rule) OR a singleton archetype whose presence gates the tick; an ArchetypeQuery only for a genuinely cross-archetype set
   LateUpdatedSystem    {:when "observe final frame state"}   ;; after camera / gameplay writes
   :role-marker         {:required  "[SystemRole(SystemRoleKind.PerFrame)] on an Update-loop class that ALSO holds an EventReader<TEvent> field"
                         :forbidden "on a class holding no EventReader field — the table/tick anchor above already decides the role"
                         :inherited :never}                   ;; a marker contradicting the shape is a compile ERROR (MarkerShapeAnalyzer); a redundant one is a graph warning today
   :state               "none across frames"                  ;; persistent → component / singleton component; this-frame-only → PreUpdate + FrameBox<T>; genuinely unavoidable → [StateAllowed("reason")], a reviewed exception
   :shared-inputs       "PreUpdate(GameState) resolve + fail-loud, carry in FrameBox<T>" ;; frame-stamped: stale reads throw, Dispose drops refs; field carries [StateAllowed]; FORBIDDEN in UniTask systems — await spans frames, the box goes stale
   :scan-diff-each-tick "god-system smell — make it reactive" ;; emit a pulse where the change happens (ARCHITECTURE → Systems, :system/split-when)
   :view-output         "push ONE value at a time"            ;; ResourceBar pattern — never build a managed snapshot inside a system
   :wiring              "concrete in installer + hand-wired in Boot.Construct"})  ;; almost always Gameplay; a system not wired into a state never runs
```
