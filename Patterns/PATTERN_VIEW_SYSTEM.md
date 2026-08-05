---
category: B
read: trigger
trigger: "before creating a MonoBehaviour view + its driving system, or wiring how a view and its system talk"
tags: [pattern, ui, view, systems]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_EVENT](PATTERN_EVENT.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
  - "[PATTERN_PERFRAME_SYSTEM](PATTERN_PERFRAME_SYSTEM.md)"
---

# Pattern — View ↔ System

A MonoBehaviour View is dumb chrome driven by its System; they talk directly: C# event in, push-to-view out, never ECS.

The View owns its VisualElements and raises intent; the System drives it. Communication between a view
and its OWN driving system is direct C# — the view raises a local event, the system subscribes; the
system pushes values back one at a time. An ECS pulse is the WRONG tool here: it is a global signal
anyone could listen to, which is exactly the traceability the direct channel removes. One approach.

Live instance: the district-build overlay — `DistrictBuildUIView` raises C# Closed/Confirmed events, its
section views (`DistrictBuildListUIView`) raise SelectionChanged, and `DistrictBuildUISystem` + the
section subsystems subscribe directly.

## Direction contract

```clojure
(def view-system-comms
  {:in   "view → local C# event → system subscribes"      ;; UI interaction (click/select) → view raises `event Action<T>`; the driving system/subsystem hooks it. The view stays store-free.
   :out  "system → push-to-view, ONE value at a time"      ;; the ResourceBar pattern — system calls view methods (Set/Add/Clear); never build a managed snapshot in the system (PATTERN_PERFRAME_SYSTEM)
   :ecs  {:only-when #{frame-boundary assembly-boundary}}   ;; a view/system pair in the SAME asmdef, same frame → C# event. An ECS pulse is for a signal that must cross a frame (deferred) or an asmdef the C# call can't reach — and a SYSTEM raises it, never the view (PATTERN_EVENT)
   :why  "every subscription is visible AT the subscriber — no global pulse anyone could listen to"})
```

## Skeleton

The view never injects the `EntityStore` and never creates entities — it only raises intent and binds values.

```csharp
public sealed class [Feature]View : MonoBehaviour
{
    // UI interaction → local C# event. The system subscribes; the view knows nothing about it.
    public event Action<[Payload]> Picked;

    private void OnClicked([Payload] value) => Picked?.Invoke(value);

    // Output: push ONE value at a time (no snapshot argument built by the system).
    public void SetValue(int value) { /* bind to a VisualElement */ }
}
```

The system hooks the view ONCE (the view outlives it) and unhooks on dispose:

```csharp
// In the driving system/subsystem — subscribe once, guard re-entry, unhook in Dispose.
if (!_hooked) { view.Picked += OnPicked; _hooked = true; }

private void OnPicked([Payload] value)
{
    // React synchronously (main thread, inside the UI callback): write ECS via AddComponent(), push to the view.
}

public override void Dispose()
{
    if (_hooked && view != null) view.Picked -= OnPicked;
    base.Dispose();
}
```

## Rules

```clojure
(def view-system-rules
  {:view       {:is "MonoBehaviour, dumb" :never #{"inject the EntityStore" "create entities" "raise an ECS pulse to its own system"}}
   :in         "local C# event (`event Action<T>`); the driving system/subsystem subscribes directly"
   :out        "push-to-view, one value at a time — never a managed snapshot built in the system (PATTERN_PERFRAME_SYSTEM)"
   :subscribe  {:once "guard with a bool — the view outlives the system" :unhook "in Dispose"}
   :handler    "runs synchronously in the UI callback (main thread): ECS writes via AddComponent(), then push to the view"
   :ecs-pulse  {:only-when "the signal crosses a frame or an asmdef boundary the C# call can't reach"}  ;; then a SYSTEM raises the pulse (PATTERN_EVENT), never the view — e.g. a UI command into another domain
   :traceability "subscription lives at the subscriber; grep the C# event → every listener"})
```

## Anti-patterns

| Wrong | Why | Right |
|---|---|---|
| View injects the `EntityStore` and raises an ECS pulse its own system consumes | A global pulse anyone can listen to; the wiring is invisible | View raises a C# event; the system subscribes directly |
| System builds a snapshot (list/array) and hands it to the view | Managed allocation in a system (zero-alloc ban); the copy goes stale | Push one value at a time; the view holds the render state |
| Handler defers the ECS write to the next tick via a flag | Reintroduces a poll; the whole point of the C# event was directness | Handle synchronously in the callback — it is already on the main thread |
