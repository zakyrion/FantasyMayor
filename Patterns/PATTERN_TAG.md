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
    // Discriminator: present on every entity of this table; combine with a key component in With<>() queries.
    public struct [Name]Tag
    {
    }
}
```

## Rules

```clojure
(def tag-rules
  {:add             "entity.Set(new [Name]Tag()) — at creation, or an atomic stage SWAP"  ;; swap = Remove<OldTag> + Set(new NewTag) same tick (PATTERN_TRANSACTION_ENTITY); exactly-1 holds throughout
   :per-entity      {:exactly 1}                            ;; Tag Law; the only extras: structural EventTag on pulses, category UITag
   :query-table     "With<[Key]Component>().With<[Name]Tag>().AsMultiMap<[Key]Component>()" ;; key + discriminator, NEVER a bare key (Table Rule)
   :never-state     "a marker toggled at runtime is a …StateComponent (enum) — see Tag Law"  ;; Set/Remove of the SAME tag = state, not identity
   :never-kind      "a subtype marker inside a table family is a …KindComponent (enum)"      ;; a second tag breaks 1-entity-1-tag
   :naming          {:suffix "…Tag" :requires :field-less}  ;; the moment it needs a value it is a component (PATTERN_COMPONENT), not a tag
   :naming-prefix   :none                                   ;; namespace carries the domain; Table-Rule discriminators are the exception (ECS_CONVENTIONS → Naming & Construction)
   :producers+consumers ecs-graph})                         ;; do not enumerate tags in module docs
```
