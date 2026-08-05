---
category: B
read: trigger
trigger: "before creating an ECS tag (field-less marker / table discriminator)"
tags: [pattern, ecs, tags]
related:
  - "[ARCHITECTURE](../ARCHITECTURE.md)"
  - "[PATTERN_COMPONENT](PATTERN_COMPONENT.md)"
---

# Pattern — ECS Tag

A tag is an **empty `struct`** that marks an entity. It carries no data; its presence IS the information.
ONE use: the table **discriminator** — the entity's identity ("this row is an X"). Every entity carries
EXACTLY one (Tag Law, ECS_CONVENTIONS → Table Rule). One approach.

## Skeleton

```csharp
namespace Domains.[Domain].[Feature].Tags
{
    // Discriminator: part of this table's declared archetype, alongside its key component.
    public struct [Name]Tag : ITag
    {
    }
}
```

## Rules

```clojure
(def tag-rules
  {:add             "named in the table's archetype — Tags.Get<[Name]Tag>() inside its holder method"  ;; the row is BORN with it (ECS_CONVENTIONS → Declared Archetypes); tagging a live entity does not happen anywhere in this codebase — it would migrate the row out of its archetype
   :per-entity      {:exactly 1}                            ;; Tag Law; the only extras: structural EventTag on pulses, category UITag
   :query-table     "[Domain]Archetypes.[Table](store)"     ;; key + discriminator declared ONCE and reused for birth and filter, NEVER a bare key (Table Rule)
   :never-state     "a marker toggled at runtime is a …StateComponent (enum) — see Tag Law"  ;; a tag cannot be toggled: composition is fixed at birth, so a swap means delete + recreate
   :never-kind      "a subtype marker inside a table family is a …KindComponent (enum)"      ;; a second tag breaks 1-entity-1-tag
   :naming          {:suffix "…Tag" :requires :field-less}  ;; the moment it needs a value it is a component (PATTERN_COMPONENT), not a tag
   :naming-prefix   :none                                   ;; namespace carries the domain; Table-Rule discriminators are the exception (ECS_CONVENTIONS → Naming & Construction)
   :producers+consumers ecs-graph})                         ;; do not enumerate tags in module docs
```
