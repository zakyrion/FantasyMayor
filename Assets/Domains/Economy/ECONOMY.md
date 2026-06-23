---
category: A
read: reference
tags: [economy, ecs, domain]
related:
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
status: partial
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
- **Economy→Actors dependency is live.** `ResourceInitSpawnSystem` reads the City/Mayor id components and
  attaches them as OwnerFK, so `Domains.Economy` now references `Domains.Actors` (one-directional, acyclic).
  This is the deliberate dependency direction: putting resource-spawn in Economy (which reads the stable
  Actors identities) avoids the Actors→Economy↔Economy→Actors asmdef cycle that the inverse — each actor
  spawning its own resources — would create.
- **Action Points data lives in Economy, not Actors.** The Mayor's starting Action Points are authored in
  `MayorConfig` (Resource sub-domain) and published via `MayorConfigComponent`, because AP is economic data
  consumed by Economy systems. Putting it in Actors would force `Actors → Economy` and reintroduce the
  `Actors ↔ Economy` cycle (Economy already depends on Actors). For now AP is **config-data only** — no
  Action Points pool component or system exists yet; applying it to the Mayor is a later slice.

## Current State
- **Resource** — data types (`ResourceComponent`, `ResourceTag`, `ResourceType` enum) PLUS the config flow
  and spawn stage. `MayorConfigLoaderSystem` (Config Loader, `ConfigLoadStep`) loads the `MayorConfig` SO
  from Addressables, validates it (no duplicate / `Unknown` `ResourceType`, `StartActionPoints >= 0`) and
  publishes the flattened `MayorConfigComponent`. `ResourceInitSpawnSystem` (Pipeline Stage, priority 1000,
  runs after `ActorsSpawnSystem`) then gives **every City and Mayor** a full loadout — one stack per
  `ResourceType` — via the stateless `ResourceLoadoutSpawner` helper (generic over the owner FK type). The
  **Mayor's** starting amounts come from `MayorConfigComponent` (`ResourceType`s the author omits start at
  0); the **City** starts every stack at 0 (no City config yet). **Noble loadouts are NOT built**: Nobles
  emerge during play, so their resources come from a separate reactive system (`ResourceNobleSpawnSystem`
  on a payload-less `NobleSpawnEvent`) — DEFERRED until the Noble actor exists. Design recorded in
  `ECONOMY_ACTORS.canvas`.

SCAFFOLD parts still pending:
- **District identity** (`District/`) — `DistrictIdComponent` (PK, int), `DistrictTag` (discriminator),
  and `DistrictIdAllocatorComponent` (world-component id source). Data only: nothing sets the allocator
  on the world and no entity is created. District creation is **player-action-driven** (reactive), so it
  does NOT follow the world-init spawn model of `ActorsSpawnSystem` — the build-flow arrives in a later
  slice. Future district columns (omitted here): `HexIdComponent` FK, OwnerFK, Type, Price, Actions, and
  Buildings carrying `DistrictId` as a FK.

Planned archetypes: see `ECS_REFERENCE.md` (`Resource` and `District`, both marked SCAFFOLD).
