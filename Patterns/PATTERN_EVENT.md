---
category: B
read: trigger
trigger: "before creating a one-frame ECS event (pulse)"
tags: [pattern, ecs, events]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_CLEANUP_SYSTEM](PATTERN_CLEANUP_SYSTEM.md)"
---

# Pattern — One-Frame Event (Pulse)

An event is a **payload-less `struct`** raised on its own entity for exactly one frame. It says "something
changed — re-read the world", nothing more. A reactive system consumes it and reconciles; the cleanup pass
disposes it at end of tick. One approach.

## Skeleton

```csharp
namespace Domains.[Domain].[Feature].Events
{
    // Payload-less pulse: the consumer reconciles from current world state, not from data on the event.
    public struct [Name]Event
    {
    }
}
```

## Raising it (from a system, a game state, or a MonoBehaviour view)

```csharp
var pulse = world.CreateEntity();
pulse.Set(new [Name]Event());
pulse.Set(new EventTag());   // marks it one-frame; the cleanup pass disposes it at end of tick
```

## Rules

- **Payload-less.** No coordinates, lists, or ids inside the event. The consumer reads world state and
  reconciles (idempotent) — see [PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md). If a change has
  persistent truth, store the truth in a world component / on an entity; the event only says "re-read it".
- **Tolerated exception:** a tiny *identifying* payload (e.g. which item was clicked) is acceptable when the
  target cannot be derived from state — but prefer writing the target to a world component + a payload-less
  pulse. Never put bulk or derived data on an event.
- **Always `Set(new EventTag())`** alongside the event so the cleanup pass disposes the entity that tick
  (see [PATTERN_CLEANUP_SYSTEM](PATTERN_CLEANUP_SYSTEM.md)).
- **One-frame events do NOT survive the async map-creation pipeline.** Startup bulk work is a
  [pipeline stage](PATTERN_PIPELINE_STAGE.md), never an event.
- Naming: `...Event`, in `Events/`. Producer→consumer flow lives in `ecs-graph` (`/ecs-graph`).
```
