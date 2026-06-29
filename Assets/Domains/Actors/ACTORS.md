---
category: A
read: reference
tags: [actors, ecs, domain]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ECONOMY](../Economy/ECONOMY.md)"
  - "[ACTIONS](../Actions/ACTIONS.md)"
status: partial
---

# Actors

Game-rule domain owning actor identities **and their startup composition**. First domain under
`Assets/Domains/` (precedent for the root).

## Purpose
Actors are the agents that own and act in the game (City, Mayor, and later Noble, Population). Other
domains never store an actor `Entity` handle — they carry an actor **id component** as a foreign key.
This domain owns each actor's identity AND what it starts with: it creates the City and Mayor and seeds
their starting state (resource loadout, and the Mayor's Action Points + config). To attach resource
loadouts it depends on `Economy` (`Actors → Economy`); see Design Decisions.

## Non-Obvious Invariants
- An actor id component (`CityIdComponent`, `MayorIdComponent`) is the **primary key on the actor row**
  AND the **same component is the foreign key** any other entity carries to say "owned by this actor".
  There is no polymorphic `OwnerId` field and no unified `ActorId` — ownership is SoA: the owner's id
  component is itself the FK (and the join discriminator). One id type per actor kind.
- Id generation lives in **world components** (`CityIdAllocatorComponent`, `MayorIdAllocatorComponent`),
  never in a static counter or a system field. `Next` is the only mutable state, and it lives on the world.
- Each per-actor spawn stage is **idempotent via its own allocator's presence**: `CitySpawnSystem`
  returns immediately if `CityIdAllocatorComponent` exists, `MayorSpawnSystem` if
  `MayorIdAllocatorComponent` exists. A pipeline re-entry (or a future load flow that pre-sets the
  allocators) never spawns duplicate actors.
- Mayor is a **singleton actor**: `MayorIdComponent` is always `1`. `MayorIdAllocatorComponent` is kept
  only for symmetry with City and the save/load contract; it yields a constant.
- Querying an actor table or resolving an OwnerFK obeys the Table Rule — `With<CityIdComponent>` +
  `With<CityTag>` (never a bare key). See `ARCHITECTURE.md` → "Relational Modeling — Table Rule".
- `Actors → Economy`, never the reverse. Actors depends on Economy's owner-agnostic substrate
  (`ResourceComponent`, `ResourceType`, the generic `ResourceLoadoutSpawner`) to attach loadouts.
  Owner-keyed logic (which actor owns which stacks) lives HERE, not in Economy — the invariant that
  keeps the DAG acyclic.

## Design Decisions
- **Feature-first layout** (same convention as Economy): split by actor (`City/`, `Mayor/`, later
  `Noble/`, `Population/`), each keeping its `Components`/`Tags`/`Systems`/… inside; namespaces follow
  (`Domains.Actors.City.*`). Per-actor spawn lives in the actor's own `Systems/` folder
  (`City/Systems/CitySpawnSystem`, `Mayor/Systems/MayorSpawnSystem`), and the Mayor's config + loader in
  `Mayor/` too. Only `ActorsInstaller` (`Installer/`) is cross-actor at the domain root. One asmdef
  covers the whole domain.
- Per-type ids (not one shared `ActorId`): in SoA the distinguishing fact "owned by a city vs a mayor"
  IS which id component is attached, so a single polymorphic owner key would be a step backwards.
- **Per-actor spawn, not one shared `ActorsSpawnSystem`.** Each actor has its own one-shot **Pipeline
  Stage** (`IPrioritizedUniTaskSystem<MapGenerationStep>`): `CitySpawnSystem` (900), `MayorSpawnSystem`
  (910). Each creates its identity row AND attaches its starting state in one place. This is Open-Closed:
  a new actor kind adds a new stage, no shared system is edited. Stages are auto-collected by DI as the
  interface and run by `MapCreation` — `Boot` stays the init engine, no `Boot` wiring change.
- **Mayor startup state is seeded from `MayorConfig` at spawn.** `MayorConfigLoaderSystem` (Config
  Loader, `ConfigLoadStep`) loads + validates the SO and publishes `MayorConfigComponent`;
  `MayorSpawnSystem` reads it to seed the Mayor's resource loadout, the per-turn `MayorAPRestoreComponent`,
  and the Mayor's initial `ActionPoint` resource stack — all from `StartActionPoints`. Config + loader live
  with the Mayor because that state is actor-intrinsic.
- Allocator save/load is a **contract only** for now — the counter is shaped to persist, but Easy Save 3
  wiring is deferred to a later slice.

## Current State
City + Mayor are functional: PK id components, discriminator tags, the two id allocator world
components, both actor config flows (`MayorConfig` + `MayorConfigLoaderSystem` → `MayorConfigComponent`;
`CityConfig` + `CityConfigLoaderSystem` → `CityConfigComponent`), and the two per-actor spawn stages.
`CitySpawnSystem` creates the City and seeds its resource loadout from `CityConfigComponent` (resources
only — the City has no Action Points). `MayorSpawnSystem` creates the Mayor, seeds the per-turn
`MayorAPRestoreComponent` from `StartActionPoints`, attaches the inventory loadout, and seeds the Mayor's
initial `ActionPoint` resource stack (= `StartActionPoints`) — the **live AP pool is now a resource stack**,
not a component value. Both actors call Economy's generic `ResourceLoadoutSpawner` (which now excludes
`ActionPoint` from the generic loadout). AP is **restored each turn** by the Actions-domain phase
`MayorActionPointsRestoreSubSystem`; AP **spending** mechanics are NOT built yet. Noble and Population are NOT built; the
Noble loadout spawn is deferred (reactive, on a `NobleSpawnEvent`). Archetypes: see the ecs-graph (`/ecs-graph`)
(`City`, `Mayor`, `Resource`).
