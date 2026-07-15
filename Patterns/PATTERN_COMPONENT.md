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
    // Payload component: runtime data carried by an entity (or a world singleton).
    public struct [Name]Component
    {
        public [FieldType] [FieldName];
    }
}
```

A component used as a **table key** (`AsMap` / `AsMultiMap`) MUST be value-comparable:

```csharp
using System;

public struct [Name]Component : IEquatable<[Name]Component>
{
    public [KeyType] Value;

    public bool Equals([Name]Component other) => Value.Equals(other.Value);
    public override bool Equals(object obj) => obj is [Name]Component other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}
```

## FK component (key-role law — ECS_CONVENTIONS → Table Rule)

A row referencing ANOTHER table's key space carries a dedicated FK type — never the owner's PK type.
The FK wraps the SAME value the owner's PK wraps; only the type (= the role) differs. Same
`IEquatable` shape as above — it is always a map key:

```csharp
using System;

// FK into the [Owner] key space: carried by rows of other tables, keys their AsMultiMap indexes.
public struct [Owner]IdFKComponent : IEquatable<[Owner]IdFKComponent>
{
    public [OwnerKeyType] Value;   // the same value type [Owner]IdComponent wraps

    public bool Equals([Owner]IdFKComponent other) => Value.Equals(other.Value);
    public override bool Equals(object obj) => obj is [Owner]IdFKComponent other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
}
```

Join = construct the FK value from the PK value at the point of use:
`multiMap.TryGetEntities(new [Owner]IdFKComponent { Value = ownerId.Value }, out var rows)`.
The FK component lives in the OWNER's feature folder, next to its PK — one space, one pair of types.

## Rules

```clojure
(def component-rules
  {:write           #{"entity.Set<T>(v)" "world.Set<T>(v)"}    ;; NEVER mutate via ref Get — Set() is what triggers reactive filters + maintained maps
   :naming-data     "…Component"                               ;; field-less marker → "…Tag" (PATTERN_TAG); one-frame pulse → "…Event" (PATTERN_EVENT)
   :naming-fk       "…FKComponent"                             ;; wraps another key space's value — the ONLY legal cross-table reference type (key-role law)
   :naming-prefix   :none                                      ;; the namespace carries the domain; FK/PK identity components are the exception (ECS_CONVENTIONS → Naming & Construction)
   :world-component :not-query-matchable                       ;; With<T>/WhenAdded<T> do NOT see it — read world.Get<T>() guarded by world.Has<T>(); storage taxonomy: ECS_CONVENTIONS
   :table-key       {:requires "IEquatable<T> + GetHashCode"}  ;; PK and FK alike; define the key pair ONCE in the owner's folder (Table Rule, ECS_CONVENTIONS)
   :fk-per-space    {:max 1}                                   ;; one component instance per type per entity — a 2-refs relationship gets its own FK type pair
   :component-shape #{roslyn ecs-graph}})                      ;; fields/types are tool-derivable — never restate them in module docs
```
