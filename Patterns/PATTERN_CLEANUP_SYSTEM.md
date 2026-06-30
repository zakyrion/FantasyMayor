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

- **Do NOT write a per-event cleanup system.** `EventCleanupSystem` is a single global system (no subclasses);
  it cleans every `EventTag` entity regardless of event type. A per-event one duplicates it.
- **`EventCleanupSystem` runs last** (highest priority). Every reactive consumer of the event must have a LOWER
  priority so it reads the pulse before cleanup disposes it (see
  [PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)).
- **Forget `EventTag` → the entity leaks**: it lives forever and `With<[Name]Event>` keeps matching it every
  frame. Always pair the event with `EventTag`.
- An entity that carries persistent data must NOT carry `EventTag` — cleanup would destroy it. Only the
  throwaway pulse entity is tagged.
```
