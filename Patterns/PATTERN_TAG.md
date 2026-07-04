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

- **Add it with `entity.Set(new [Name]Tag())`** (or `entity.Set<[Name]Tag>()`).
- **It is the table discriminator** — query the table as key + discriminator, never a bare key:
  `world.GetEntities().With<[Key]Component>().With<[Name]Tag>().AsMultiMap<[Key]Component>()` (Table Rule,
  `../ARCHITECTURE.md`).
- **A parameter-less condition uses a tag where a data kind would use a component.** When a kind carries data,
  the payload component doubles as the discriminator; when it carries none, an empty tag is the discriminator.
- Naming: `...Tag`. Keep it field-less — the moment it needs a value it is a [component](PATTERN_COMPONENT.md),
  not a tag. No domain-name prefix — the namespace carries the domain (see
  [../ECS_CONVENTIONS.md](../ECS_CONVENTIONS.md) → Naming & Construction; Table-Rule discriminators are the
  exception).
- Tags and their producers/consumers are visible in `ecs-graph` — do not enumerate them in module docs.
```
