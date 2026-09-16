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

A tag is an empty struct that marks an entity. It carries no data; its presence IS the information.
TWO uses: the main tag — the table discriminator, the entity's identity ("this row is an X"), exactly one
per archetype and unique to it; a label tag — a role or membership marker beside it, marked [TagLabel]
(ARCHITECTURE → Entities, :tag/one-main-tag).

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
  {:add             "named in the table's archetype — Tags.Get<[Name]Tag>() inside its holder method"  ;; the row is BORN with it (ARCHITECTURE → Entities, :archetype/holder); tagging a live entity does not happen anywhere in this codebase — it would migrate the row out of its archetype
   :per-entity      {:main 1 :labels "0-4"}                 ;; Tag Law; EventTag is the main tag of every event; UITag and DistrictOpenConditionTag are labels
   :label           "[TagLabel] or [TagLabel(TagLabelRole.X)] on the struct; named in Tags.Get after the main tag; never a query filter"
   :query-table     "[Domain]Archetypes.[Table](store)"     ;; key + discriminator declared ONCE and reused for birth and filter, NEVER a bare key (Table Rule)
   :never-state     "a marker toggled at runtime is a …StateComponent (enum) — see Tag Law"  ;; a tag cannot be toggled: composition is fixed at birth, so a swap means delete + recreate
   :never-kind      "a subtype marker inside a table family is a …KindComponent (enum)"      ;; a second main tag breaks the tag law; a label never stands for a kind
   :naming          {:suffix "…Tag" :requires :field-less}  ;; the moment it needs a value it is a component (PATTERN_COMPONENT), not a tag
   :naming-prefix   :none                                   ;; namespace carries the domain; Table-Rule discriminators are the exception (ARCHITECTURE → Naming)
   :producers+consumers fantasymayor-graph})                         ;; do not enumerate tags in module docs
```
