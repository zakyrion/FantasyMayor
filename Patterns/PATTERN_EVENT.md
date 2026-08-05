---
category: B
read: trigger
trigger: "before creating a one-frame ECS event (pulse)"
tags: [pattern, ecs, events]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_REACTIVE_SYSTEM](PATTERN_REACTIVE_SYSTEM.md)"
  - "[PATTERN_CLEANUP_SYSTEM](PATTERN_CLEANUP_SYSTEM.md)"
  - "[PATTERN_VIEW_SYSTEM](PATTERN_VIEW_SYSTEM.md)"
---

# Pattern — One-Frame Event (Pulse)

An event is a **payload-less `struct`** raised on its own entity. It says "something changed — re-read the
world", nothing more. EVERY reactive consumer sees it exactly once, the frame AFTER it is raised, whatever
the priorities; the cleanup pass deletes it at the end of that frame (ECS_CONVENTIONS → Event Lifecycle).
One approach.

## Skeleton

```csharp
namespace Domains.[Domain].[Feature].Events
{
    // Payload-less pulse: the consumer reconciles from current store state, not from data on the event.
    public struct [Name]Event : IComponent
    {
    }
}
```

## Raising it (from a system or a game state)

```csharp
// One creating call: frame stamp + payload + EventTag, straight into the event's archetype.
_store.CreateEvent(new [Name]Event());
```

A MonoBehaviour view does NOT raise a pulse to its own driving system — it raises a local C# event the
system subscribes to (PATTERN_VIEW_SYSTEM). A view/UI-system may raise an ECS pulse ONLY to cross a
frame or an asmdef boundary the C# call can't reach, and then a SYSTEM raises it, not the view.

## Rules

```clojure
(def event-rules
  {:payload            :none                                          ;; no coords/lists/ids — the consumer reconciles from world state (PATTERN_REACTIVE_SYSTEM); persistent truth lives in a world component / on an entity, the event only says "re-read it"
   :payload-tolerated  "tiny IDENTIFYING value"                       ;; only when the target cannot be derived from state; prefer target→world-component + payload-less pulse; NEVER bulk or derived data
   :raise              "store.CreateEvent(new [Name]Event())"        ;; the helper stamps the frame and lands the row in its archetype — never assemble a pulse by hand (EcsEventExtensions)
   :view-source        {:never "a view raising a pulse to its OWN system"       ;; use a local C# event → the system subscribes (PATTERN_VIEW_SYSTEM); an ECS pulse is only for crossing a frame/asmdef boundary, raised by a SYSTEM
                        :only  "cross a frame/asmdef boundary the C# call can't reach"}
   :startup-bulk-work  pipeline-stage                                 ;; never an event — one-frame events do NOT survive the async map-creation pipeline (PATTERN_PIPELINE_STAGE)
   :naming             {:suffix "…Event" :in "Events/"}               ;; no domain prefix — namespace carries it (ECS_CONVENTIONS → Naming & Construction)
   :visibility         "priority-independent"                         ;; EVERY consumer sees EVERY pulse exactly once, the frame after it is raised — a "consumer must sit below the producer" rule cannot exist (ECS_CONVENTIONS → Event Lifecycle)
   :latency            "1 frame per link"                             ;; a pulse chain costs a frame per hop; a consumer must never assume a same-frame reaction
   :feedback-loop      {:never "populate→command→populate on one-frame events"}  ;; each lap now costs a frame instead of deadlocking — still wrong: restructure so data flows one way (proven 2026-07-08 on the district-build draft attempt)
   :raise-thread       "main thread ONLY"                             ;; every store call is main-thread (ECS_CONVENTIONS → Law 1); off-thread compute hops back before raising
   :lossy-producer     "level-triggered doorbell"                     ;; producer that can't control its frame window (turn phase, async): RE-RAISE every turn/tick while the condition holds + consumer reconciles state, never trusts one delivery — a lost pulse costs latency, never correctness (decreed: FLOW_DISTRICT_BUILD → ordering-invariants :completion-pulse)
   :producer->consumer ecs-graph})
```
