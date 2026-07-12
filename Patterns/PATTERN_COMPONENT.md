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

A component used as a **table key** (`AsMap` / `AsMultiMap` / a shared FK) MUST be value-comparable:

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

## Rules

```clojure
(def component-rules
  {:write           #{"entity.Set<T>(v)" "world.Set<T>(v)"}    ;; NEVER mutate via ref Get — Set() is what triggers reactive filters + maintained maps
   :naming-data     "…Component"                               ;; field-less marker → "…Tag" (PATTERN_TAG); one-frame pulse → "…Event" (PATTERN_EVENT)
   :naming-prefix   :none                                      ;; the namespace carries the domain; FK/PK identity components are the exception (ECS_CONVENTIONS → Naming & Construction)
   :world-component :not-query-matchable                       ;; With<T>/WhenAdded<T> do NOT see it — read world.Get<T>() guarded by world.Has<T>(); storage taxonomy: ECS_CONVENTIONS
   :table-key       {:requires "IEquatable<T> + GetHashCode"}  ;; define the shared key ONCE, reuse everywhere (Table Rule, ECS_CONVENTIONS)
   :component-shape #{roslyn ecs-graph}})                      ;; fields/types are tool-derivable — never restate them in module docs
```
