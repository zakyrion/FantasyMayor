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

## Raising it (from a system or a game state)

```csharp
var pulse = world.CreateEntity();
pulse.Set(new [Name]Event());
pulse.Set(new EventTag());   // marks it one-frame; the cleanup pass disposes it at end of tick
```

A MonoBehaviour view does NOT raise a pulse to its own driving system — it raises a local C# event the
system subscribes to (PATTERN_VIEW_SYSTEM). A view/UI-system may raise an ECS pulse ONLY to cross a
frame or an asmdef boundary the C# call can't reach, and then a SYSTEM raises it, not the view.

## Rules

```clojure
(def event-rules
  {:payload            :none                                          ;; no coords/lists/ids — the consumer reconciles from world state (PATTERN_REACTIVE_SYSTEM); persistent truth lives in a world component / on an entity, the event only says "re-read it"
   :payload-tolerated  "tiny IDENTIFYING value"                       ;; only when the target cannot be derived from state; prefer target→world-component + payload-less pulse; NEVER bulk or derived data
   :raise              "pulse.Set(event) + pulse.Set(new EventTag())" ;; EventTag opts it into end-of-tick disposal (PATTERN_CLEANUP_SYSTEM)
   :view-source        {:never "a view raising a pulse to its OWN system"       ;; use a local C# event → the system subscribes (PATTERN_VIEW_SYSTEM); an ECS pulse is only for crossing a frame/asmdef boundary, raised by a SYSTEM
                        :only  "cross a frame/asmdef boundary the C# call can't reach"}
   :startup-bulk-work  pipeline-stage                                 ;; never an event — one-frame events do NOT survive the async map-creation pipeline (PATTERN_PIPELINE_STAGE)
   :naming             {:suffix "…Event" :in "Events/"}               ;; no domain prefix — namespace carries it (ECS_CONVENTIONS → Naming & Construction)
   :in-tick-visibility "consumer.Priority > emitter.Priority"         ;; the pulse dies at the SAME tick's EventCleanupSystem (MaxValue) — only later-priority systems see it that tick; lower-priority consumers see it NEVER
   :feedback-loop      {:never "populate→command→populate on one-frame events"}  ;; impossible at ANY priorities (proven 2026-07-08 on the district-build draft attempt) — restructure so data flows DOWN the priority order once
   :raise-thread       "main thread ONLY"                             ;; a pulse is a structural write (CreateEntity + Set) — an off-thread producer (TurnPhaseSubSystem on RunOnThreadPool) hops SwitchToMainThread first; law: ECS_CONVENTIONS → Threading And Native Memory; зразок BuildDistrictTurnTickSystem
   :lossy-producer     "level-triggered doorbell"                     ;; producer that can't control its frame window (turn phase, async): RE-RAISE every turn/tick while the condition holds + consumer reconciles state, never trusts one delivery — a lost pulse costs latency, never correctness (decreed: FLOW_DISTRICT_BUILD → ordering-invariants :completion-pulse)
   :producer->consumer ecs-graph})
```
