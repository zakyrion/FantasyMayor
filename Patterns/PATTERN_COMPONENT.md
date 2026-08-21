---
category: B
read: trigger
trigger: "before creating an ECS data component (a struct holding runtime values)"
tags: [pattern, ecs, components]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_TAG](PATTERN_TAG.md)"
---

# Pattern — ECS Data Component

A component is a plain `struct` of runtime values. No behavior, no methods (except equality when it is a
table key). One approach.

## Skeleton

```csharp
namespace Domains.[Domain].[Feature].Components
{
    // Payload component: runtime data carried by an entity or the singleton-component store.
    public struct [Name]Component : IComponent
    {
        public [FieldType] [FieldName];
    }
}
```

A component that **keys an index** declares `IIndexedComponent<TValue>` and returns the key field:

```csharp
public struct [Name]Component : IIndexedComponent<[KeyType]>
{
    public [KeyType] Value;

    public [KeyType] GetIndexedValue() => Value;   // the index is keyed on this value
}
```

`[KeyType]` must be equatable: an enum works as-is; a struct key implements `IEquatable<T>` +
`GetHashCode` itself (`HexCoord`). A PK that is only ever compared — never indexed — stays a plain
`IComponent` with `IEquatable` (worked example: `HexIdComponent`).

## FK component (key-role law — ECS_CONVENTIONS → Table Rule)

A row referencing ANOTHER table's key space carries a dedicated FK type — never the owner's PK type.
The FK wraps the SAME value the owner's PK wraps; only the type (= the role) differs. An FK is what
keyed joins run on, so it is the indexed shape:

```csharp
// FK into the [Owner] key space: carried by rows of other tables, keys their ComponentIndex.
public struct [Owner]IdFKComponent : IIndexedComponent<[OwnerKeyType]>
{
    public [OwnerKeyType] Value;   // the same value type [Owner]IdComponent wraps

    public [OwnerKeyType] GetIndexedValue() => Value;
}
```

Join = look the owner's key VALUE up in the FK index at the point of use:
`foreach (var row in _rowsByOwner[ownerId.Value]) { … }`.
The FK component lives in the OWNER's feature folder, next to its PK — one space, one pair of types.

## Rules

```clojure
(def component-rules
  {:write           #{"entity.AddComponent(v)" "storages.Singletons.Set(v)"}  ;; upsert; NEVER mutate through a ref — the write CALL is what re-files the index (ECS_CONVENTIONS → Component Writes)
   :declare         "struct : IComponent"                      ;; a component the engine cannot see is a silent no-op at birth
   :naming-data     "…Component"                               ;; field-less marker → "…Tag" (PATTERN_TAG); one-frame pulse → "…Event" (PATTERN_EVENT)
   :naming-fk       "…FKComponent"                             ;; wraps another key space's value — the ONLY legal cross-table reference type (key-role law)
   :naming-prefix   :none                                      ;; the namespace carries the domain; FK/PK identity components are the exception (ECS_CONVENTIONS → Naming & Construction)
   :singleton-component :not-query-matchable                   ;; the hidden row has a declared archetype, but consumers can only read storages.Singletons.Get<T>(); storage taxonomy: ECS_CONVENTIONS
   :index-key       {:requires "IIndexedComponent<TValue>, TValue equatable"}  ;; define the PK/FK pair ONCE in the owner's folder (Table Rule, ECS_CONVENTIONS)
   :index-bucket    {:max 100}                                 ;; entities per identical key value — insert/remove is O(N) over duplicates
   :fk-per-space    {:max 1}                                   ;; one component instance per type per entity — a 2-refs relationship gets its own FK type pair
   :birth-column    "every column is named by the archetype"   ;; adding a component to a live entity migrates it out of its archetype (ECS_CONVENTIONS → Birth Completeness)
   :component-shape #{roslyn ecs-graph}})                      ;; fields/types are tool-derivable — never restate them in module docs
```
