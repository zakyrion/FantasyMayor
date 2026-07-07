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
Two uses: a table **discriminator** ("this row is an X") and a **kind marker** ("this is the single-instance
variant"). One approach.

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
  {:add             "entity.Set(new [Name]Tag())"
   :query-table     "With<[Key]Component>().With<[Name]Tag>().AsMultiMap<[Key]Component>()" ;; key + discriminator, NEVER a bare key (Table Rule)
   :kind-with-data  "payload component doubles as discriminator"
   :kind-param-less "empty tag IS the discriminator"
   :naming          {:suffix "…Tag" :requires :field-less}  ;; the moment it needs a value it is a component (PATTERN_COMPONENT), not a tag
   :naming-prefix   :none                                   ;; namespace carries the domain; Table-Rule discriminators are the exception (ECS_CONVENTIONS → Naming & Construction)
   :producers+consumers ecs-graph})                         ;; do not enumerate tags in module docs
```
