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

**You almost never write a cleanup system.** There is ONE global `EventCleanupSystem` (`EcsExtensions`): a
per-frame system that runs LAST each tick and deletes every `EventTag` entity that is already ripe — i.e.
after every consumer has had its full frame with it. You opt an event in by raising it through the helper —
nothing else.

## How to opt an event into cleanup

```csharp
_store.CreateEvent(new [Name]Event());   // stamps the frame + EventTag → the global cleaner owns its death
```

That is the entire contract. See [PATTERN_EVENT](PATTERN_EVENT.md).

## Rules

```clojure
(def cleanup-rules
  {:per-event-cleanup      :never-write-one            ;; EventCleanupSystem is the single global cleaner (no subclasses) — a per-event one duplicates it
   EventCleanupSystem      "runs last, int.MaxValue"   ;; it deletes only RIPE events, so consumer priority no longer matters (ECS_CONVENTIONS → Event Lifecycle)
   :hand-rolled-pulse      "the entity LEAKS"          ;; assembling a pulse without CreateEvent misses EventTag and/or the frame stamp: no stamp = never ripe = never cleaned; always raise via the helper
   :persistent-data-entity {:never-carries EventTag}   ;; cleanup would destroy it — only the throwaway pulse entity carries the tag
   :sweep-is-cross-archetype "EventTag, by design"})   ;; the one filter that deliberately spans archetypes — every event type at once (ECS_CONVENTIONS → Declared Archetypes :filter)
```
