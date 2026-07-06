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

```lisp
(payload           → none)                                 ;; no coords/lists/ids — the consumer reconciles from world state (PATTERN_REACTIVE_SYSTEM); persistent truth lives in a world component / on an entity, the event only says "re-read it"
(payload :tolerated → tiny IDENTIFYING value)              ;; only when the target cannot be derived from state; prefer target→world-component + payload-less pulse; NEVER bulk or derived data
(raise             → pulse.Set(event) + pulse.Set(new EventTag()))  ;; EventTag opts it into end-of-tick disposal (PATTERN_CLEANUP_SYSTEM)
(startup-bulk-work → pipeline-stage, never an event)       ;; one-frame events do NOT survive the async map-creation pipeline (PATTERN_PIPELINE_STAGE)
(naming            → "…Event" :in Events/)                 ;; no domain prefix — namespace carries it (ECS_CONVENTIONS → Naming & Construction)
(producer→consumer → ecs-graph)
```
