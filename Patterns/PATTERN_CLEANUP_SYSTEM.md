---
category: B
read: trigger
trigger: "before writing any one-frame-event cleanup (and to learn why you usually should not)"
tags: [pattern, ecs, systems, events]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
---

# Pattern — One-Frame Event Cleanup

**You almost never write a cleanup system.** There is ONE global `EventCleanupSystem` (DefaultECSExtensions): a
per-frame system that runs LAST each tick and disposes every entity carrying `EventTag`. To make an event
one-frame, you opt it in by tagging — nothing else.

## How to opt an event into cleanup

When you raise the event, tag the entity:

```csharp
var pulse = world.CreateEntity();
pulse.Set(new [Name]Event());
pulse.Set(new EventTag());   // ← the global EventCleanupSystem disposes this entity at end of tick
```

That is the entire contract. See [PATTERN_EVENT](PATTERN_EVENT.md).

## Rules

```lisp
(per-event-cleanup → NEVER write one)                      ;; EventCleanupSystem is the single global cleaner (no subclasses) — a per-event one duplicates it
(EventCleanupSystem → runs last, int.MaxValue)             ;; every reactive consumer must have LOWER priority to read the pulse before disposal (PATTERN_REACTIVE_SYSTEM)
(forget-EventTag   → the entity LEAKS)                     ;; lives forever, With<[Name]Event> keeps matching every frame — always pair event + EventTag
(persistent-data-entity :never-carries EventTag)           ;; cleanup would destroy it — only the throwaway pulse entity is tagged
```
