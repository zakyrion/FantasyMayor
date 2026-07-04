---
category: C
read: reference
tags:
  - plan
  - district
  - actions
  - wip
related:
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[ACTIONS](Assets/Domains/Actions/ACTIONS.md)"
  - "[BUILD_DISTRICT_COST](Assets/Domains/Economy/DistrictBuildCost/BUILD_DISTRICT_COST.md)"
  - "[BUILD_DISTRICT_OUTCOME](Assets/Domains/Economy/DistrictBuildOutcome/BUILD_DISTRICT_OUTCOME.md)"
  - "[BUILD_DISTRICT_ACTION](Assets/Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md)"
  - "[DISTRICT_OPEN_CONDITION](Assets/Domains/Economy/DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
---

# District-Build Action — Program Plan (FM-10)

> **WIP program doc.** Multi-session. Delete when the program completes. Each step is one short session.
> Check off steps as they land; keep "Open items" current so any later session resumes without re-deriving context.

## Context

`Economy.DistrictOpenCondition` already answers **"may this district type be built?"** (`DistrictCanBeBuildTag`),
and `Actions.DISTRICT_BUILD_COST` already loads **"what does it cost?"** (read by the DistrictBuild UI). Missing is
the **build verb itself**: turning a player's confirmed choice into a live, multi-turn construction that completes
and produces a result. No Actions verb spends the cost or places anything yet.

This program adds that verb as **two ECS entity families with DIFFERENT shapes** (they do **not** share a template):

1. **`BuildDistrictAction`** — a **live, runtime, one-row-per-pending-build** action. Born a *draft* when the
   DistrictBuild window opens, edited piecewise by the UI subsystems/views, **committed** on confirm (deduct
   AP/resources, start countdown) or **discarded** on close. `ActionTurnLeftComponent` decrements each turn; at zero
   a system Sets `ActionCompleteTag`. Built as a **live-lifecycle entity** — plain components/tags
   (`PATTERN_COMPONENT` / `PATTERN_TAG`) + reactive / turn-phase systems. **NOT** the polymorphic-catalogue pattern.
2. **`BuildDistrictOutcome`** — an **authored polymorphic catalogue** (a literal `DistrictOpenCondition` clone,
   spawn/routing half only): one outcome-spec per `DistrictType`, materialized into an entity table via
   `PATTERN_POLYMORPHIC_CATALOGUE`. On completion, the completed action's `DistrictType` **joins** (`AsMap`) into
   this table and the matching outcome runs. First concrete kind: **spawn City Center**.

**Naming (decided).** Canonical spelling `Build` (canvas's `Buid` is a typo). Per `ECS_CONVENTIONS.md` → Naming &
Construction, type names carry **no `Actions` domain prefix** (the namespace does); the two features are
`BuildDistrictAction` and `BuildDistrictOutcome` (`Result` → `Outcome` to avoid colliding with the generic
`Result<T>` in `Addressable`). The generic action-lifecycle components (`ActionIdComponent`, `ActionAPCostComponent`,
`ActionResourcePriceComponent`, `ActionTurnLeftComponent`, `ActionCompleteTag`) keep the singular **`Action`**
concept prefix — that is the shared action key space, not the domain folder. Execution is **step-by-step across
short sessions**; Step 1 = Pattern extraction first.

## Reference skeleton (from `Economy/DistrictOpenCondition/` — the shape `BuildDistrictOutcome` copies)

- **Config side:** container SO (`DistrictOpenConditionsConfig` holds `DistrictOpenConditionConfig[]`) → abstract base
  keyed by `DistrictType` → concrete subclasses (payload-carrying **or** parameter-less) → loader at `ConfigLoadStep`
  publishes a world component holding the **live SO reference** (retains the addressable `Box`, releases on dispose).
- **Spawn family (routing):** orchestrator `IPrioritizedUniTaskSystem<MapGenerationStep>` iterates the catalogue and
  `TrySpawn(config)` down a DI-collected `IReadOnlyList<…SpawnSubSystem>`, **fails loud** on null/unhandled type; each
  concrete subsystem creates one entity: FK key (`DistrictTypeComponent`) + discriminator tag + per-kind marker
  (payload **component** if it has params, else empty **tag**).
- **Evaluator family (non-routing loop):** two host wrappers (`MapGenerationStep` bootstrap for turn 1 +
  `TurnPhaseSubSystem` for later turns) run the **same** DI-collected `IReadOnlyList<…EvaluatorSubSystem>`; each
  self-queries its kind-slice (`AsSet`) and idempotently Sets/Removes an output tag, joining the target table via
  `EntityMultiMap<DistrictTypeComponent>` (`AsMultiMap`, Table Rule).  *(`BuildDistrictOutcome` uses only the spawn half.)*
- **DI:** every system/subsystem `Singleton` in the domain installer; subsystems registered `.As<AbstractBase>()` for
  `IReadOnlyList<T>` collection injection; `[StateAllowed]` on the collected field.

---

## ▶ Step 1 — Extract the Pattern *(execute first)*  — ✅ DONE (`Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md`)

New Category-B recipe **`Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md`** — "Polymorphic Config Catalogue → Entity Table
(+ optional per-kind evaluator)". Documents the reference skeleton above as a reusable procedure: the five moving
parts; **routing vs non-routing** orchestrator variants; **per-kind discriminator rule** (payload → component,
parameter-less → marker tag); **dual-host reuse**; **Table-Rule join** via `AsMap`/`AsMultiMap`; DI collection-injection
shape; a "when NOT to use" (single non-polymorphic config → plain `PATTERN_CONFIG`). Cross-link `PATTERN_CONFIG`,
`PATTERN_CONFIG_LOADER`, `PATTERN_ORCHESTRATOR_SUBSYSTEM`, `PATTERN_PIPELINE_STAGE`, `PATTERN_TAG`.

Plumbing: add a row to **`ARCHITECTURE.md` → Pattern Recipes**; run `python3 Tools/gen_index.py` (pass 1). Frontmatter
`category: B`, `read: trigger`, a `trigger:` line. **No code.**

---

## ▶ Step 2 — `BuildDistrictOutcome` catalogue (authored, spawn-only)

New sub-area **`Assets/Domains/Actions/BuildDistrictOutcome/`** — a clean `DistrictOpenCondition` clone, routing/spawn
half only (no evaluator; it's a static lookup table joined at apply-time in Step 5).

**Files (mirror the reference skeleton 1:1):**
- `Configs/BuildDistrictOutcomeConfig.cs` — `abstract class : ScriptableObject`; `_districtType : DistrictType`
  → `DistrictType` property (the **key** = which district this outcome belongs to).
- `Configs/BuildDistrictOutcomesConfig.cs` — container SO; `BuildDistrictOutcomeConfig[] Outcomes` property.
- `Configs/SpawnDistrictOutcomeConfig.cs` — first concrete kind; **parameter-less** (the `DistrictType` key already
  says which district to spawn) → so its per-kind discriminator is a **marker tag**.
- `Components/BuildDistrictOutcomesConfigComponent.cs` — world component `{ BuildDistrictOutcomesConfig Value }`
  (live SO ref, not flattened).
- `Tags/BuildDistrictOutcomeTag.cs` — discriminator ("this row is a build-outcome").
- `Tags/SpawnDistrictOutcomeTag.cs` — per-kind marker (parameter-less kind).
- `Systems/BuildDistrictOutcomesConfigLoaderSystem.cs` — `: ConfigLoaderSystem`; addressable key
  `"BuildDistrictOutcomesConfig"`; retains the `Box`, validates all `Outcomes` non-null (**fail loud**), Sets the
  world component, releases in `OnDispose`. Shape = `DistrictOpenConditionsConfigLoaderSystem`.
- `Systems/BuildDistrictOutcomeSpawnSystem.cs` — orchestrator `: IPrioritizedUniTaskSystem<MapGenerationStep>`;
  ctor `(World world, IReadOnlyList<BuildDistrictOutcomeSpawnSubSystem> subSystems)`; `[StateAllowed]` on the list
  field; reads the world component, iterates `Outcomes[]`, `TrySpawn` each, **fail-loud** on null entry / unhandled type.
  **Priority: 930** (in the domain-spawn cluster, after DistrictOpenCondition spawn 920 / bootstrap 925 — added **last**;
  confirm no clash via `mcp__ecs-graph__execution_order` at build time).
- `Systems/BuildDistrictOutcomeSpawnSubSystem.cs` — `internal abstract : IDisposable`; `protected World`;
  `bool IsEnabled`, `int Priority`; `abstract bool TrySpawn(BuildDistrictOutcomeConfig config)`.
- `Systems/SpawnDistrictOutcomeSubSystem.cs` — concrete; matches `SpawnDistrictOutcomeConfig`; creates entity:
  `DistrictTypeComponent` (FK) + `BuildDistrictOutcomeTag` (discriminator) + `SpawnDistrictOutcomeTag` (marker).

**`BuildDistrictOutcome` archetype:** `DistrictTypeComponent` + `BuildDistrictOutcomeTag` + `SpawnDistrictOutcomeTag`.
**DI (`ActionsInstaller` — installer keeps its domain prefix, the documented exception):** loader
`.As<IUniTaskSystem<ConfigLoadStep>>()`; orchestrator `.As<IPrioritizedUniTaskSystem<MapGenerationStep>>()`; subsystem
`.As<BuildDistrictOutcomeSpawnSubSystem>()`. All `Singleton`. (Registration reference:
`mcp__di-graph__installer_registrations ActionsInstaller`.)
**Authored asset** (`.asset` container + per-outcome assets + addressable key) is owned outside code (user authors);
until authored, the loader fails loud at boot — expected, same as `DistrictOpenConditionsConfig`.
**No apply/execution here** — this step only materializes the lookup table; "spawn City Center" runs in Step 5.

---

## ▶ Step 3 — `BuildDistrictAction` data structures (no systems)

New sub-area **`Assets/Domains/Actions/BuildDistrictAction/`**, `Components/` + `Tags/` + `Data/` only. Build per
`PATTERN_COMPONENT` / `PATTERN_TAG`. **No systems** (those are Step 4). ecs-graph refresh deferred to Step 6.

**Files:**
- `Components/ActionIdComponent.cs` — PK; `{ int Value }`, `IEquatable<ActionIdComponent>`. (Shared action key space —
  keeps the `Action` prefix, the FK/PK exception. ID **allocation** is a Step-4 concern — the creating system assigns
  it; here just the type.)
- `Tags/BuildDistrictActionTag.cs` — discriminator ("this row is a build-district action").
- `Components/ActionAPCostComponent.cs` — `{ int Value }` (AP price snapshot).
- `Data/ResourcePriceSlot.cs` — `struct { ResourceType Type; int Amount; }` (one price line). *(Confirm the exact
  existing `ResourceType` enum + whether an existing `ResourceComponent` already is this shape — reuse if so.)*
- `Components/ActionResourcePriceComponent.cs` — **fixed 6-slot inline** struct: six named `ResourcePriceSlot`
  fields (`Price1..Price6`) + `int Count` (used slots) + `this[int]` indexer (switch over the six fields). **No
  lists/arrays** — zero-alloc translation of `List<ResourceComponent> _districtPrices` on `DistrictBuildConfig`.
- `Components/ActionTurnLeftComponent.cs` — `{ int Value }` (turns remaining until finished).
- `Tags/ActionCompleteTag.cs` — "completed; apply the outcome now".

**Reused FK components (do NOT redefine — Set them in Step 4):** `HexIdComponent` (Map), `DistrictTypeComponent`
(Economy/District), and the existing **owner-FK** component (City/Mayor/Noble). *(Owner-FK exact type name unconfirmed —
resolve via `mcp__ecs-graph__find_node` / ECONOMY_ACTORS.canvas before Step 4; the canvas labels it "OwnerFK:
CityId | MayorId | NobleId".)*

**`BuildDistrictAction` full archetype (for reference, assembled in Step 4):** `ActionIdComponent` (PK) +
`BuildDistrictActionTag` (discriminator) + `HexIdComponent` (FK) + `DistrictTypeComponent` (FK) + owner-FK +
`ActionAPCostComponent` + `ActionResourcePriceComponent` + `ActionTurnLeftComponent` [+ `ActionCompleteTag` when done].

---

## ▶ Step 4 — `BuildDistrictAction` lifecycle systems

Splits into draft → edit → commit/discard → tick → complete. **Rule (ACTIONS.md):** UI orchestrates; the **mechanic
lives in the Actions domain**, not in the view.

**(a) Draft creation.** `DistrictBuildUISystem` (Presentation.UI) creates the draft action entity when
`DistrictBuildUIView` opens — Sets `ActionIdComponent` (allocate the PK here) + `BuildDistrictActionTag`.
*(Confirm current `DistrictBuildUISystem` / `DistrictBuildUIView` API + where the open hook is — DISTRICT_BUILD.md +
DISTRICT_BUILDING_UI.canvas.)*
**(b) Piecewise edits.** As the player picks district / hex / owner, the DistrictBuild view-subsystems `Set` their own
components on the draft: `DistrictTypeComponent`, `HexIdComponent`, owner-FK; and **snapshot the cost** from
`BuildDistrictCostsConfig` (joined by `DistrictType`) into `ActionAPCostComponent` + `ActionResourcePriceComponent`.
**(c) Commit (`_confirmButton`).** Raise a one-frame commit pulse; an **Actions** verb system reacts: validate
affordability, **deduct** AP + resources from the owner's inventory (reuse/add an owner-agnostic Economy spend
helper — *confirm whether one exists*; math stays in Economy, sequencing in Actions), seed `ActionTurnLeftComponent`
from the config's **`TurnsToBuild`** (added in commit `5f5cb5a`; confirm its home). The entity is now a committed
pending build.
**(d) Discard (`_closeButton` without confirm).** Delete the draft entity (dispose).
**(e) Tick + complete.** An Actions `TurnPhaseSubSystem` decrements `ActionTurnLeftComponent` for every committed
action each turn; at `Value == 0` Sets `ActionCompleteTag`. Reuse the `Turn` pipeline (TURN.md / TURN_PHASES.md);
turn-phase priority TBD relative to existing phases.

**Open items to resolve at build time:** owner-FK component name; whether an Economy "spend resources for owner N"
helper exists (else add one, owner-agnostic); `TurnsToBuild`'s config home + accessor; `DistrictBuildUISystem`/view
open/confirm/close hook points; turn-phase ordering; the commit-pulse mechanism (one-frame event vs tag). *(The
DISTRICT_BUILD_COST configs have since been renamed with a `Cost` disambiguator — see the Naming-cleanup section
below; this plan uses the post-rename names.)*

---

## ▶ Step 5 — Apply outcome (complete → join → spawn City Center)

Reactive system on `ActionCompleteTag`: read the completed action's `DistrictType` → `AsMap<DistrictTypeComponent>`
join into the `BuildDistrictOutcome` table → run the matching outcome subsystem. First concrete: **`SpawnDistrictOutcome`**
spawns the City Center district entity (into the `District` table — `DistrictTag` + `DistrictTypeComponent`, currently
scaffold per DISTRICT_OPEN_CONDITION.md). Then dispose the completed action entity. *(This also makes the
`DistrictSingleOpenCondition` evaluator meaningful — once a CityCenter exists, its `DistrictCanBeBuildTag` flips off.)*

## ▶ Step 6 — Docs + graph refresh (permissioned)

Per-sub-area MDs (`BUILD_DISTRICT_ACTION.md`, `BUILD_DISTRICT_OUTCOME.md`) per `DOC_STANDARD.md`; refresh ecs-graph +
di-graph; re-run `gen_index.py`. **After** code lands → STOP and ask before syncing (docs-sync needs permission).

---

## Naming-cleanup session — ✅ DONE (Unity compile pending)

Project-wide audit (all `Domains`) confirmed the no-domain-prefix rule is already honoured almost everywhere.
**7 types renamed** (installers keep `[Domain]Installer`; FK/PK, `MapGenerationStep`, and
`ActorType`/`ActorTypeComponent` kept as concept/identity). The Actions cost catalogue could **not** be stripped
bare — that collides with Economy's identically-named gating catalogue
(`Domains.Economy.District.*.DistrictsBuildConfig{,Component,LoaderSystem}`) — so it took the **`Cost`** disambiguator:

| Was | Now | Note |
|---|---|---|
| `ActionsDistrictsBuildConfig` | `DistrictsBuildCostConfig` | Actions — cost catalogue container |
| `ActionsDistrictBuildConfig` | `DistrictBuildCostConfig` | Actions — per-district cost |
| `ActionsDistrictsBuildConfigComponent` | `DistrictsBuildCostConfigComponent` | Actions — world component |
| `ActionsDistrictsBuildConfigLoaderSystem` | `DistrictsBuildCostConfigLoaderSystem` | Actions — loader |
| `ActionsDistrictActionConfig` | `DistrictActionConfig` | Actions — near-empty stub |
| `MapGenerationSystem` | `GenerationSystem` | Map/Generation — orchestrator |
| `MapGenerationSubSystem` | `GenerationSubSystem` | Map/Generation — subsystem base |

Done via `rename_symbol` (all refs) + `git mv` of each `.cs`+`.meta` pair (GUIDs preserved → `.asset` bindings intact).
**Not touched:** the addressable-key `const "ActionsDistrictsBuildConfig"` (the authored `.asset` binds to it) — the
`BuildDistrictCostsConfig` class still loads from address `"ActionsDistrictsBuildConfig"`; align later, Unity-side.
**Pending:** Unity must regenerate its gitignored `.csproj`/`.sln` and compile — the source is consistent; roslyn's
stale project files report false "type not found" until then.

## Naming — verb-first C#/FDG pass + sub-domain split — ✅ DONE (Unity compiled)

Superseding the `Cost`-disambiguated names above: the naming rule was flipped to the **C# / .NET FDG** camp
(`ECS_CONVENTIONS.md` → Naming & Construction — *self-sufficient names, no stutter-stripping*; `ARCHITECTURE.md`
pointer updated), and the flat Actions domain was split into **sub-domains** (folder = namespace segment).
Verb-first renames:

| Was | Now | Home (namespace) |
|---|---|---|
| `DistrictBuildCostConfig` | `BuildDistrictCostConfig` | `Domains.Actions.BuildDistrictCost.Configs` |
| `DistrictsBuildCostConfig` | `BuildDistrictCostsConfig` | `Domains.Actions.BuildDistrictCost.Configs` |
| `DistrictsBuildCostConfigComponent` | `BuildDistrictCostsConfigComponent` | `Domains.Actions.BuildDistrictCost.Components` |
| `DistrictsBuildCostConfigLoaderSystem` | `BuildDistrictCostsConfigLoaderSystem` | `Domains.Actions.BuildDistrictCost.Systems` |
| `DistrictActionConfig` | `BuildDistrictActionConfig` | `Domains.Actions.BuildDistrictAction.Configs` (Step-3 seed) |

`MayorAPRestoreSubSystem` stays in `Actions/Systems/` — AP-restore is not promoted to its own sub-domain (too
thin for a single system; revisit when a second related turn phase lands). Addressable key still untouched.
**Steps 2–5 below use the verb-first names** (`BuildDistrictOutcome*`, `BuildDistrictAction*`).

## Catalogues → Economy + district-first flip — ✅ DONE (2026-07-04)

Cost + Outcome catalogues moved **Actions → Economy** (they are owner-agnostic district **vocabulary**, keyed
by `DistrictType` — siblings of `DistrictOpenCondition`), and the type names flipped **verb-first →
district-first**. **`BuildDistrictAction` (the verb) stays in Actions** — it READS cost/outcome but does not own
them. Current district-first inventory (supersedes the verb-first names in Steps 2/5 above):

| Verb-first (was) | District-first (now) | Home (namespace) |
|---|---|---|
| `BuildDistrictOutcomeConfig` | `DistrictBuildOutcomeConfig` | `Domains.Economy.DistrictBuildOutcome.*` |
| `BuildDistrictOutcomesConfig` | `DistrictBuildOutcomesConfig` | `Domains.Economy.DistrictBuildOutcome.*` |
| `SpawnDistrictOutcomeConfig` | `SpawnCityCenterOutcomeConfig` | first concrete PER-DISTRICT kind |
| `SpawnDistrictOutcomeTag` | `SpawnCityCenterOutcomeTag` | per-kind marker |
| `BuildDistrictOutcomesConfigComponent` | `DistrictBuildOutcomesConfigComponent` | world/config component |
| `BuildDistrictOutcomesConfigLoaderSystem` | `DistrictBuildOutcomesConfigLoaderSystem` | loader |
| `BuildDistrictOutcomeSpawnSystem` | `DistrictBuildOutcomeSpawnSystem` | orchestrator (prio 930) |
| `BuildDistrictOutcomeSpawnSubSystem` | `DistrictBuildOutcomeSpawnSubSystem` | subsystem base |
| `SpawnDistrictOutcomeSubSystem` | `SpawnCityCenterOutcomeSubSystem` | concrete subsystem |
| `BuildDistrictOutcomeTag` | `DistrictBuildOutcomeTag` | discriminator |

The cost catalogue kept its district-first names (`DistrictBuildCostConfig`, `DistrictBuildCostsConfig`,
`DistrictBuildCostsConfigComponent`, `DistrictBuildCostsConfigLoaderSystem`) and moved to
`Domains.Economy.DistrictBuildCost.*`. Design intent: EACH district gets its OWN `Spawn<District>OutcomeConfig`
subclass + its own subsystem (Open-Closed). Addressable keys unchanged: cost still loads from the legacy
`"ActionsDistrictsBuildConfig"`; outcome from `"DistrictBuildOutcomesConfig"`.
> Steps 2 and 5 above (still verb-first, and still placing the outcome catalogue in `Actions/`) are prior
> history — read them through this table; the outcome catalogue now lives in `Economy/DistrictBuildOutcome/`.
