---
category: A
read: reference
tags: [economy, ecs, domain]
related:
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
status: scaffold
---

# Economy

Game-rule domain owning economic objects: inventory resources now; districts and buildings later.

## Purpose
Holds the resources, and eventually the districts/buildings, that actors own and that the turn economy
acts on. This slice ships only the **inventory resource** data types — no systems.

## Non-Obvious Invariants
- **Inventory resources are owner-scoped stacks, distinct from Hex resources.** Module `HexResources`
  owns the natural per-hex resource layer (Forest / Clay / Fish, keyed by `HexIdComponent`). This domain's
  `ResourceType` (`Grain`, `Clay`, `Wood`) is a **different enum in a different namespace** — `Clay`
  appears in both with unrelated meaning. Do not confuse or join them.
- Ownership is SoA: a resource entity carries the **owner's id component** (`CityIdComponent` |
  `MayorIdComponent`) as its foreign key, plus `ResourceTag` as the table discriminator. There is no
  polymorphic owner field.
- Identity is the **composite key `(owner FK + ResourceType)`** — there is deliberately **no surrogate
  `ResourceId`**. One stack per (owner, type).
- A given owner's stacks are read with `With<OwnerFK>` + `With<ResourceTag>` → `AsMultiMap<OwnerFK>`,
  never a bare key (the owner id is a PK on the actor AND a FK here). See `ARCHITECTURE.md` → Table Rule.

## Design Decisions
- **Feature-first layout.** Economy is split by sub-system (`Resource/`, later `District/`, `Building/`),
  not type-first at the domain root. Each feature keeps the `Components`/`Tags`/`Data`/`Systems` split
  inside it, and namespaces follow (`Domains.Economy.Resource.*`). One asmdef still covers the whole domain.
- **Data-only this slice.** No spawner and no systems yet, so the domain does **not** reference `Actors`
  yet. The Economy→Actors foreign-key dependency (and the first real OwnerFK attachment) lands with the
  resource-spawn system in a later slice.

## Current State
SCAFFOLD. Only the data types exist: `ResourceComponent` (type + amount), `ResourceTag`, and the
`ResourceType` enum. No resource entity is created at runtime and no system touches them yet. Planned
archetype: see `ECS_REFERENCE.md` (`Resource`, marked SCAFFOLD).
