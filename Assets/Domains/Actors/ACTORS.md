---
category: A
read: reference
tags: [actors, ecs, domain]
related:
  - "[ECS_REFERENCE](../../../ECS_REFERENCE.md)"
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ECONOMY](../Economy/ECONOMY.md)"
status: scaffold
---

# Actors

Game-rule domain owning actor identities. First domain under `Assets/Domains/` (precedent for the root).

## Purpose
Actors are the agents that own and act in the game (City, Mayor, and later Noble, Population). Other
domains never store an actor `Entity` handle — they carry an actor **id component** as a foreign key.
This slice ships only the City and Mayor identities.

## Non-Obvious Invariants
- An actor id component (`CityIdComponent`, `MayorIdComponent`) is the **primary key on the actor row**
  AND the **same component is the foreign key** any other entity carries to say "owned by this actor".
  There is no polymorphic `OwnerId` field and no unified `ActorId` — ownership is SoA: the owner's id
  component is itself the FK (and the join discriminator). One id type per actor kind.
- Id generation lives in **world components** (`CityIdAllocatorComponent`, `MayorIdAllocatorComponent`),
  never in a static counter or a system field. `Next` is the only mutable state, and it lives on the world.
- `ActorsSpawnSystem` is **idempotent via the allocator's presence**: if `CityIdAllocatorComponent`
  already exists the stage returns immediately, so a pipeline re-entry (or a future load flow that
  pre-sets the allocators) never spawns duplicate actors.
- Mayor is a **singleton actor**: `MayorIdComponent` is always `1`. `MayorIdAllocatorComponent` is kept
  only for symmetry with City and the save/load contract; it yields a constant.
- Querying an actor table or resolving an OwnerFK obeys the Table Rule — `With<CityIdComponent>` +
  `With<CityTag>` (never a bare key). See `ARCHITECTURE.md` → "Relational Modeling — Table Rule".

## Design Decisions
- **Feature-first layout** (same convention as Economy): split by actor (`City/`, `Mayor/`, later
  `Noble/`, `Population/`), each keeping its `Components`/`Tags`/… inside; namespaces follow
  (`Domains.Actors.City.*`). **Cross-actor** code stays at the domain root — `ActorsSpawnSystem`
  (`Systems/`) and `ActorsInstaller` (`Installer/`) span all actors, so they belong to no single actor
  folder. One asmdef still covers the whole domain.
- Per-type ids (not one shared `ActorId`): in SoA the distinguishing fact "owned by a city vs a mayor"
  IS which id component is attached, so a single polymorphic owner key would be a step backwards.
- `ActorsSpawnSystem` is a one-shot **Pipeline Stage** (`IPrioritizedUniTaskSystem<TerrainGenerationStep>`,
  priority 900) run by `MapCreation` — actor creation is world-init, not per-frame or reactive. It is
  auto-collected by DI as the interface; no `Boot` wiring change.
- Allocator save/load is a **contract only** for now — the counter is shaped to persist, but Easy Save 3
  wiring is deferred to a later slice.

## Current State
SCAFFOLD. Only City + Mayor identities exist: their PK id components, discriminator tags, the two id
allocator world components, and `ActorsSpawnSystem` (creates one City and one Mayor during map creation).
Noble and Population are NOT built. Nothing reads the actors yet — the first OwnerFK consumer arrives
with the Economy resource-spawn system. Archetypes: see `ECS_REFERENCE.md` (`City`, `Mayor`).
