---
category: A
read: reference
tags: [economy, ecs, domain]
related:
  - "[ARCHITECTURE](../../../ARCHITECTURE.md)"
  - "[ACTORS](../Actors/ACTORS.md)"
  - "[DISTRICT_OPEN_CONDITION](DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
  - "[BUILD_DISTRICT_COST](DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
status: partial
code_refs:
  components:       [ResourceComponent, DistrictIdComponent]
  tags:             [ResourceTag, DistrictTag]
  world_components: [DistrictsBuildConfigComponent, DistrictIdAllocatorComponent]
  systems:          [DistrictsBuildConfigLoaderSystem]
  configs:          [DistrictsBuildConfig, DistrictBuildingConfig]
  enums:            [ResourceType]
  installers:       [EconomyInstaller]
  helpers:          [ResourceLoadoutSpawner]
---

# Economy

Game-rule domain owning economic objects: inventory resources now; districts and buildings later.

## Purpose
Holds the resources, and eventually the districts/buildings, that actors own and that the turn economy
acts on. This slice ships the **inventory resource** data types plus the generic, owner-agnostic
`ResourceLoadoutSpawner` mechanism, and the **owner-agnostic district vocabulary** — four sibling catalogues,
all keyed by `DistrictType`: placement (`DistrictsBuildConfig`), unlock (`DistrictOpenCondition/`), **cost**
(`DistrictBuildCost/`), **outcome** (`DistrictBuildOutcome/`). Economy owns NO actor knowledge and NO actor
spawn/config systems — those moved to `Actors` (see Design Decisions); the district-catalogue config loaders
stay here.

Sub-domains that are district vocabulary (each keyed on `DistrictType`, none knowing about an owner):
- **`DistrictOpenCondition/`** → [DISTRICT_OPEN_CONDITION.md](DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md) —
  unlock rules ("may this district be built?"); polymorphic catalogue + spawn + per-turn evaluator.
- **`DistrictBuildCost/`** → [BUILD_DISTRICT_COST.md](DistrictBuildCost/BUILD_DISTRICT_COST.md) — per-district
  AP + resource price (loaded at `ConfigLoadStep`, read by the DistrictBuild UI and by the Actions build verb).
- **`DistrictBuildOutcome/`** → [BUILD_DISTRICT_OUTCOME.md](DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md) —
  the build result per district (spawn City Center first); polymorphic catalogue, spawn-only (no evaluator).

The owner-scoped build **verb** that reads cost/outcome lives in `Actions.BuildDistrictAction`, not here.

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
  never a bare key (the owner id is a PK on the actor AND a FK here). See `ECS_CONVENTIONS.md` → Table Rule.

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
  (resources + AP) is actor-intrinsic and belongs with the actor. `StartActionPoints` is seeded as the
  Mayor's `ActionPoint` **resource stack** (`ResourceType`) at spawn — the former dedicated
  `MayorAPComponent` was superseded; AP **spending mechanics** remain a later slice.
