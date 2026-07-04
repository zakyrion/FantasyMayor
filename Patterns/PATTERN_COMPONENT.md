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

- **Write via `entity.Set<T>(value)` / `world.Set<T>(value)`** — never mutate through `ref Get`. `Set` is what
  triggers reactive filters and maintained maps; a silent `ref` mutation breaks them.
- **Naming:** `...Component` for data; `...Tag` for a field-less marker (see [PATTERN_TAG](PATTERN_TAG.md));
  one-frame event is `...Event` (see [PATTERN_EVENT](PATTERN_EVENT.md)). No domain-name prefix — the
  namespace carries the domain (see [../ECS_CONVENTIONS.md](../ECS_CONVENTIONS.md) → Naming & Construction;
  FK/PK identity components are the exception).
- **Entity vs world component:** the same struct can live on an entity (`entity.Set`) or as a world singleton
  (`world.Set`). A world component is **not** query-matchable (`With<T>` / `WhenAdded<T>` do not see it) — read
  it with `world.Get<T>()` guarded by `world.Has<T>()`. Full storage taxonomy in `../ECS_CONVENTIONS.md`.
- **Key components need `IEquatable<T>` + `GetHashCode`** so they can key a table; define the shared key once
  and reuse it everywhere (Table Rule, `../ECS_CONVENTIONS.md`).
- Component shape (fields, types) is recovered by `roslyn-mcp` / `ecs-graph` — do not restate it in module docs.
```
