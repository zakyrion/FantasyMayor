---
category: B
read: trigger
trigger: "before creating an ECS event"
tags: [pattern, ecs, events]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_VIEW_SYSTEM](PATTERN_VIEW_SYSTEM.md)"
---

# Pattern — Event (log)

An event is an entity in the `Events` store carrying exactly one component whose type implements
`IEventTag`; it lives in its type's ring until a newer event past capacity evicts it. EVERY reader sees
it exactly once, in the tick it is raised or the next one, depending on tick order (ARCHITECTURE → Events).
One approach.

## Skeleton

```csharp
namespace Domains.[Domain].[Feature].Events
{
    // The event's fields are ordinary data: the consumer acts on them or reconciles against store state.
    // [EventCapacity(n)] is optional — a type without it gets EventCapacityAttribute.DefaultCapacity (128).
    public struct [Name]Event : IEventTag
    {
        public [ValueType] [Value];
    }
}
```

Add one line for the new type to `EventReaderAotDeclarations.DeclareClosedReaders` (rule `event/aot-reader`)
so IL2CPP generates the closed `EventReader<[Name]Event>` it needs.

## Raising it (from a system, a game state, or a view crossing a boundary)

```csharp
// One call: the log stamps the global sequence, rings the event under its own type, evicts the oldest
// of that type if the ring is already at capacity.
_storages.Events.Raise(new [Name]Event { [Value] = value });
```

A MonoBehaviour view does NOT raise an event to its own driving system — it raises a local C# event the
system subscribes to (PATTERN_VIEW_SYSTEM). A view/UI-system may raise a log event ONLY to cross a
frame or an asmdef boundary the C# call can't reach, and then a SYSTEM raises it, not the view.

## Rules

```clojure
(def event-rules
  {:values             "ordinary data the consumer needs"             ;; the consumer acts on them directly or reconciles against store state (PATTERN_REACTIVE_SYSTEM)
   :raise              "_storages.Events.Raise(new [Name]Event { … })"  ;; the log stamps the sequence and rings the entity — never AddComponent a TEvent anywhere else (event/raise)
   :view-source        {:never "a view raising an event to its OWN system"     ;; use a local C# event → the system subscribes (PATTERN_VIEW_SYSTEM); a log event is only for crossing a frame/asmdef boundary, raised by a SYSTEM
                        :only  "cross a frame/asmdef boundary the C# call can't reach"}
   :startup-bulk-work  pipeline-stage                                 ;; never an event — an event carries no work order and is not guaranteed to be read before eviction (PATTERN_PIPELINE_STAGE)
   :naming             {:suffix "…Event" :in "Events/"}               ;; no domain prefix — namespace carries it (ARCHITECTURE → Naming)
   :capacity           "[EventCapacity(n)] on the struct, else 128"    ;; a new event past capacity evicts the oldest of its type — overflow never throws (event/log-capacity)
   :visibility         "delivery follows the tick"                    ;; a reader after its producer's priority sees the event the same tick; a reader before it sees it the next tick — never "must sit above/below the producer" (event/delivery)
   :feedback-loop      {:never "populate→command→populate on log events"}  ;; a consumer never raises the type it reads in the same loop (event/one-way)
   :raise-thread       "main thread ONLY"                             ;; every store call is main-thread (ARCHITECTURE → Threading and structural change); off-thread compute hops back before raising
   :aot-reader         "closed EventReader<TEvent> declared in EventReaderAotDeclarations"  ;; otherwise an IL2CPP player never births that reader (event/aot-reader)
   :clear-all          "EventLog.ClearAllEvents wipes every ring"      ;; cursors catch up to empty on their next read; who calls it is a decision for whichever task needs it (event/clear-all)
   :producer->consumer fantasymayor-graph})
```
