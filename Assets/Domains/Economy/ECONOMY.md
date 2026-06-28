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
acts on. This slice ships the **inventory resource** data types plus the generic, owner-agnostic
`ResourceLoadoutSpawner` mechanism, and the **District build catalogue** config flow. Economy owns NO actor
knowledge and NO actor spawn/config systems — those moved to `Actors` (see Design Decisions); its own
District-catalogue config loader stays here.

## Non-Obvious Invariants
- **Inventory resources are owner-scoped stacks, distinct from Hex resources.** Module `HexResources`
  owns the natural per-hex resource layer (Forest / Clay / Fish, keyed by `HexIdComponent`). This domain's
  `ResourceType` (`Grain`, `Clay`, `Wood`, …) is a **different enum in a different namespace** — `Clay`
  appears in both with unrelated meaning. Do not confuse or join them.
- **`ResourceType.ActionPoint` is a resource value but NOT a generic-loadout member.** Action Points are
  modelled as an inventory stack (the live AP pool), but only AP owners hold one. `ResourceLoadoutSpawner.SpawnLoadout`
  therefore SKIPS `ActionPoint`; AP owners (Mayor; later Important Citizens) seed it explicitly via
  `ResourceLoadoutSpawner.SpawnResource`. The City has no AP stack.
- Ownership is SoA: a resource entity carries the **owner's id component** (`CityIdComponent` |
  `MayorIdComponent`) as its foreign key, plus `ResourceTag` as the table discriminator. There is no
  polymorphic owner field. Economy never NAMES an owner id type — `ResourceLoadoutSpawner<TOwnerId>`
  attaches it generically and the concrete owner is supplied by the caller (`Actors`). That is what
  keeps Economy owner-agnostic.
- Identity is the **composite key `(owner FK + ResourceType)`** — there is deliberately **no surrogate
  `ResourceId`**. One stack per (owner, type).
- A given owner's stacks are read with `With<OwnerFK>` + `With<ResourceTag>` → `AsMultiMap<OwnerFK>`,
  never a bare key (the owner id is a PK on the actor AND a FK here). See `ARCHITECTURE.md` → Table Rule.

## Design Decisions
- **Feature-first layout.** Economy is split by sub-system (`Resource/`, later `District/`, `Building/`),
  not type-first at the domain root. Each feature keeps the `Components`/`Tags`/`Data`/`Systems` split
  inside it, and namespaces follow (`Domains.Economy.Resource.*`). One asmdef still covers the whole domain.
- **Economy is the owner-agnostic substrate leaf; `Actors → Economy` (not the reverse).** Economy holds
  no owner-keyed query and never references `Domains.Actors`. The cross-domain glue — creating actors and
  attaching their resource loadout — lives in `Actors`, which calls Economy's generic
  `ResourceLoadoutSpawner`. This deliberately INVERTS the earlier `Economy → Actors` direction: it stops
  Economy from becoming the sink every actor feature has to extend (Open-Closed). Invariant: **any
  owner-keyed logic stays in `Actors`/`Actions`, never in Economy** — that is what keeps the DAG acyclic.
  (`Economy → Map`, for District hex-type gating, is a separate, unrelated edge.)
- **Mayor config (incl. Action Points) lives in `Actors`, not Economy.** `MayorConfig` /
  `MayorConfigComponent` / `MayorConfigLoaderSystem` moved to `Actors/Mayor` — the Mayor's starting state
  (resources + AP) is actor-intrinsic and belongs with the actor. `StartActionPoints` is seeded onto
  `MayorAPComponent` at spawn; AP **pool/spending mechanics** remain a later slice.

## Current State
- **Resource** — data types (`ResourceComponent`, `ResourceTag`, `ResourceType` enum) plus the stateless
  generic `ResourceLoadoutSpawner` helper. `SpawnLoadout` creates one stack per `ResourceType` for a given
  owner FK (excluding `Unknown` and `ActionPoint`); `SpawnResource` creates a single stack for the
  owner-specific cases excluded from the loadout (e.g. the Mayor's `ActionPoint` pool). That helper is the
  only logic Economy ships — it has **no systems**. The config flow and per-actor loadout spawn now live in
  `Actors` (`MayorConfig*` + `City/MayorSpawnSystem`, which call `ResourceLoadoutSpawner`). The Mayor's amounts come from `MayorConfigComponent` and the City's from
  `CityConfigComponent` (`ResourceType`s the author omits start at 0). Noble loadouts are
  still DEFERRED (Nobles emerge during play — a reactive spawn in `Actors`, on a payload-less
  `NobleSpawnEvent`, lands with the Noble actor). Design recorded in `ECONOMY_ACTORS.canvas`.

- **District build catalogue** (`District/`) — the buildable-district config flow is live:
  `DistrictsBuildConfigLoaderSystem` (Config Loader, `ConfigLoadStep`) loads the `DistrictsBuildConfig` SO
  (address `"DistrictsBuildConfig"`), validates it, and publishes the world component
  `DistrictsBuildConfigComponent`, which carries a **reference** to the SO (no copy/flatten — the SO already
  holds the `DistrictBuildingConfig[]` + their prices/requirements). The loader **retains the addressable Box**
  for the catalogue's lifetime (the build window reads it throughout play) and releases it in `OnDispose`. This
  is Economy's first system; registered in `EconomyInstaller`.

SCAFFOLD parts still pending:
- **District identity** (`District/`) — `DistrictIdComponent` (PK, int), `DistrictTag` (discriminator),
  and `DistrictIdAllocatorComponent` (world-component id source). Data only: nothing sets the allocator
  on the world and no entity is created. District creation is **player-action-driven** (reactive), so it
  does NOT follow the world-init spawn model of the per-actor `City/MayorSpawnSystem` — the build-flow arrives in a later
  slice. Future district columns (omitted here): `HexIdComponent` FK, OwnerFK, Type, Price, Actions, and
  Buildings carrying `DistrictId` as a FK.

Planned archetypes: see `ECS_REFERENCE.md` (`Resource` and `District`, both marked SCAFFOLD).
