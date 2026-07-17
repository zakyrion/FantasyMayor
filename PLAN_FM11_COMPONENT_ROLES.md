---
category: C
read: reference
tags: [plan, fm-11, ecs, refactoring]
related:
  - "[ECS_CONVENTIONS](ECS_CONVENTIONS.md)"
  - "[PATTERN_COMPONENT](Patterns/PATTERN_COMPONENT.md)"
  - "[PATTERN_TAG](Patterns/PATTERN_TAG.md)"
  - "[PATTERN_POLYMORPHIC_CATALOGUE](Patterns/PATTERN_POLYMORPHIC_CATALOGUE.md)"
---

# PLAN FM-11 — Component key-role migration (PK / FK / Data + tag classes)

Executor plan: split shared keys into PK + FK types and convert kind/state tags to enum components.

> **Temporary multi-session plan artifact (2026-07-15, rev 2).** Deleted when the program completes
> (the record then = commit history). Written for a Sonnet-class executor: every decision is already
> closed; an executor that meets a mismatch between this plan and the code STOPS and asks — it never
> improvises.

<!-- doc-lint: off — this plan is FULL of FM-11 target-state type names created during the migration -->

## Status ledger — tick after the step's DoD passes

- [x] S1 — City space (`CityIdFKComponent`)
- [x] S2 — Mayor space (`MayorIdFKComponent`)
- [x] S3 — District catalogue (`DistrictTypeFKComponent` + kind/state enum components, 3 tags deleted)
- [ ] S4a — Hex space, domain half (`HexIdFKComponent`; Map + Actions)
- [ ] S4b — Hex space, presentation half (views, icons, UI panels)
- [ ] S5 — Sweep & closure (comments, doc-lint wrappers, graphs, this file's fate)

## Decreed decisions (ALL closed — the executor decides NOTHING)

```clojure
(def fm11-decisions  ;; user-confirmed 2026-07-15
  {:scope        "PK↔FK conflations (Hex/City/Mayor id) + the District catalogue (FK + kind + state)"
   :fk-suffix    "…FKComponent"                              ;; HexIdFKComponent, CityIdFKComponent, MayorIdFKComponent, DistrictTypeFKComponent
   :fk-home      "the OWNER's Components/ folder, next to its PK"
   :kind-spaces  {:owner-side :data :referencing-side :fk}   ;; District FACT keeps DistrictTypeComponent; catalogue/verb rows carry DistrictTypeFKComponent
   :tag-law      "EXACTLY 1 tag per entity and per filter"   ;; decreed in ECS_CONVENTIONS → Table Rule + ARCHITECTURE
   :kind         "…KindComponent {enum Value} — never a second tag"   ;; per-kind access = self-index AsMultiMap + TryGetEntities(kind)
   :state        "…StateComponent {enum Value} — never a toggled tag" ;; change-only Set(); Set re-indexes the state self-index automatically
   :self-index   :legal                                      ;; Data/kind/state may key a map when the discriminator is the SAME table's tag (IDX)
   :pk-untouched true                                        ;; …IdComponent types keep their names, fields, IEquatable
   :docs         "already decreed — Table Rule (key-role law + tag law), PATTERN_COMPONENT, PATTERN_TAG, PATTERN_POLYMORPHIC_CATALOGUE"})
```

New-type names below are fixed (`:by-naming-policy`, user has veto until a step starts):
`DistrictOpenConditionKind` / `DistrictOpenConditionKindComponent`, `DistrictOpenState` /
`DistrictOpenStateComponent`, `DistrictBuildOutcomeKind` / `DistrictBuildOutcomeKindComponent`.

## DefaultEcs facts (verified against 0.17.2 source — the design rests on these)

- `AsMap<TKey>` / `AsMultiMap<TKey>` key on the component TYPE and implicitly add `With<TKey>()`;
  values group via `EqualityComparer<TKey>.Default` → every FK/kind/state component implements
  `IEquatable<T>` + `GetHashCode`.
- Maps self-maintain (`ComponentChangedMessage<TKey>`): `entity.Set(...)` re-indexes (a state flip
  moves the row between slices), removal/dispose drops the entry. `EntityMap` THROWS on a duplicate
  key — PK uniqueness is fail-loud for free.
- ONE component instance per type per entity ⇒ max ONE FK into a given space per entity.
- Lookup takes a constructed value: `map.TryGetEntities(new HexIdFKComponent { Coords = pk.Coords }, out var rows)`.

## Progress meter (run after EVERY step)

`python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py spaces` (key-role conflations) and
`… ecsg.py tags` (tag-law deviations). Start of program: **3 conflations + 12 tag deviations**.
Targets per step are in each DoD; program end: **0 + 0**.

## KNOWN LIVE BUG the migration fixes by design

All EIGHT owner-resource multimap chains (4 files × City+Mayor) require the ACTOR's tag that
resource rows never carry (`ResourceLoadoutSpawner.SpawnResource` sets only: owner id +
`ResourceComponent` + resource tag). They are permanently EMPTY today — the resource bar never
fills, district-build affordability is always false. The TARGET chains in S1/S2 therefore carry
exactly ONE tag (the resource tag). Do NOT re-add `With<CityTag>` / `With<MayorTag>` to any
resource chain.

## The mechanical recipe (applies to every step)

1. **Create each new type** in the folder the step names:
   - FK: copy the owner PK's shape exactly (same field, same `IEquatable` pattern), name = PK name
     with `FK` before `Component`. Header comment:
     `// FK into the <Owner> key space: carried by rows of other tables, keys their AsMultiMap indexes.`
   - Kind/state enums go in the feature's `Data/` folder (`Unknown = 0` sentinel first, per
     `ResourceType` precedent); their components in `Components/`, wrapping `Value`, `IEquatable`,
     `GetHashCode() => (int)Value`.
2. **Classify every site** mentioning the old type in the step's files:
   - chain contains the OWNER's tag (`HexTag`, `CityTag`, `MayorTag`) → PK side — DO NOT TOUCH;
   - row/chain belongs to ANOTHER table (view rows, resource rows, transaction, catalogue, fact)
     → FK side — swap the type.
3. **FK-side rewrite**, all three shapes:
   - `entity.Set(new XIdComponent {...})` → `entity.Set(new XIdFKComponent {...})`;
   - `.With<XIdComponent>()…AsMultiMap<XIdComponent>()` → same chain with the FK type
     (field type `EntityMultiMap<XIdFKComponent>`);
   - lookups: construct the FK from the PK value — `new XIdFKComponent { <Field> = pk.<Field> }`;
     reads FROM an FK row: `row.Get<XIdFKComponent>()`.
4. **State writes are change-only**: read the current value, `Set()` only when it differs — never
   unconditional writes, never `Has`+`Remove` toggles.
5. **Comments in the same diff**: any comment on a touched line that names the old tag/PK-as-FK
   scheme → reword to the new type.
6. A site in a step's file that matches NONE of the listed patterns → STOP, report it, wait.

## S1 — City space

New file: `Assets/Domains/Actors/City/Components/CityIdFKComponent.cs` (wraps `int Value`).

| File | Sites | Side |
|---|---|---|
| `Assets/Domains/Actors/City/Systems/CitySpawnSystem.cs` | `:59` `SpawnLoadout<CityIdComponent, CityResourceTag>(_world, cityIdComponent, …)` → `SpawnLoadout<CityIdFKComponent, CityResourceTag>(_world, new CityIdFKComponent { Value = cityId }, …)` | FK |
| same file | `:53-57` actor row (`city.Set(cityIdComponent)` + `CityTag` + `ActorTypeComponent`) | PK — untouched |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs` | field `:53`, chain `:68-69` → `.With<CityIdFKComponent>().With<CityResourceTag>().AsMultiMap<CityIdFKComponent>()` (drop `With<CityTag>`), lookup `:149` construct FK from `_cityActor…Get<CityIdComponent>().Value` | FK |
| same file | `:64-65` `_cityActor` set (`CityTag`+`ActorTypeComponent`) | PK — untouched |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs` | field `:46`, chain `:71-72` (same rewrite), lookup `:143` | FK |
| `Assets/Presentation/UI/MainHud/ResourceBar/Systems/ResourceBarSystem.cs` | field `:31`, chain `:41-42`, `FillCity` `:75-85` (keep the `CityIdComponent owner` parameter; construct the FK at the `TryGetEntities` call), comment `:30` | FK |
| `Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildPriceUISubSystem.cs` | field `:41`, chain `:57-58`, lookup `:199` | FK |
| comments | `Assets/Domains/Actors/City/Tags/CityResourceTag.cs`, `Assets/Domains/Economy/Resource/Components/ResourceComponent.cs:7`, `Assets/Domains/Economy/Resource/Helpers/ResourceLoadoutSpawner.cs:9` (generic helper itself is UNCHANGED — the FK arrives as the type argument), `Assets/Domains/Actors/City/Components/CityIdComponent.cs:5` header ("…same component is the foreign key…" — delete that clause) | — |

DoD: `ecsg.py spaces` → City clean, 2 conflations remaining; `ecsg.py tags` → the 4 City map-chain
warnings gone (8 tag deviations remaining); `grep -rn "AsMultiMap<CityIdComponent>" Assets` → 0;
roslyn diagnostics on edited files (CS0246 on `CityIdFKComponent` from sibling files = expected
workspace staleness, name it once); user-side Unity check — the resource bar's CITY row filling up
is the visible proof.

## S2 — Mayor space (mirror of S1)

New file: `Assets/Domains/Actors/Mayor/Components/MayorIdFKComponent.cs` (wraps `int Value`).

| File | Sites | Side |
|---|---|---|
| `Assets/Domains/Actors/Mayor/Systems/MayorSpawnSystem.cs` | `:63` `SpawnLoadout<MayorIdFKComponent, MayorResourceTag>(_world, new MayorIdFKComponent { Value = mayorId }, …)`; actor row `:55-58` untouched | FK |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs` | field `:52`, chain `:66-67` → `.With<MayorIdFKComponent>().With<MayorResourceTag>().AsMultiMap<MayorIdFKComponent>()` (drop `With<MayorTag>`), lookup `:140` | FK |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionCancelSystem.cs` | field `:45`, chain `:69-70`, lookup `:134` | FK |
| `Assets/Presentation/UI/MainHud/ResourceBar/Systems/ResourceBarSystem.cs` | field `:32`, chain `:43-44`, `FillMayor` `:87-97` | FK |
| `Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildPriceUISubSystem.cs` | field `:40`, chain `:55-56`, lookup `:192` | FK |
| PK — untouched | `MayorAPRestoreSubSystem.cs:28`, `TurnPanelViewSystem.cs:41`, `DistrictBuildPriceUISubSystem.cs:53`, `ResourceBarSystem.cs:39` (actor chains with `MayorTag`) | — |
| comments | `Assets/Domains/Actors/Mayor/Tags/MayorResourceTag.cs`, `Assets/Domains/Actors/Mayor/Components/MayorAPRestoreComponent.cs:4`, `Assets/Domains/Actors/Mayor/Components/MayorIdComponent.cs:5` header | — |

DoD: `spaces` → 1 conflation remaining; `tags` → 4 tag deviations remaining;
`grep -rn "AsMultiMap<MayorIdComponent>" Assets` → 0; Unity check: mayor row + AP on the resource bar.

## S3 — District catalogue: FK + kind column + state column

The kind space has NO PK table — the `DistrictType` enum value is the key. Owner-side carriers
(a district IS of the type) keep `DistrictTypeComponent`; referencing rows (a rule/verb ABOUT the
type) switch to the FK. Kind tags and the state tag become enum components (Tag Law).

**New files:**

| File | Content |
|---|---|
| `Assets/Domains/Economy/District/Components/DistrictTypeFKComponent.cs` | FK, wraps `DistrictType Value` |
| `Assets/Domains/Economy/DistrictOpenCondition/Data/DistrictOpenConditionKind.cs` | `enum { Unknown = 0, SingleOpen, Exist }` |
| `Assets/Domains/Economy/DistrictOpenCondition/Components/DistrictOpenConditionKindComponent.cs` | wraps the kind enum, `IEquatable` |
| `Assets/Domains/Economy/DistrictOpenCondition/Data/DistrictOpenState.cs` | `enum { Unknown = 0, Closed, Buildable }` |
| `Assets/Domains/Economy/DistrictOpenCondition/Components/DistrictOpenStateComponent.cs` | wraps the state enum, `IEquatable` |
| `Assets/Domains/Economy/DistrictBuildOutcome/Data/DistrictBuildOutcomeKind.cs` | `enum { Unknown = 0, SpawnCityCenter }` |
| `Assets/Domains/Economy/DistrictBuildOutcome/Components/DistrictBuildOutcomeKindComponent.cs` | wraps the kind enum, `IEquatable` |

**Deleted files** (delete each `.cs` together with its paired `.meta`):
`Assets/Domains/Economy/DistrictOpenCondition/Tags/DistrictSingleOpenConditionTag.cs`,
`Assets/Domains/Economy/DistrictOpenCondition/Tags/DistrictCanBeBuildTag.cs`,
`Assets/Domains/Economy/DistrictBuildOutcome/Tags/SpawnCityCenterOutcomeTag.cs`.
(`DistrictOpenConditionTag` and `DistrictBuildOutcomeTag` STAY — they are the identity tags.)

**Row rewrites (spawns):**

| File | Target row |
|---|---|
| `…/DistrictSingleOpenConditionSpawnSubSystem.cs` `:27-29` | FK + `DistrictOpenConditionTag` + kind `SingleOpen` + state `Closed` |
| `…/DistrictExistConditionSpawnSubSystem.cs` `:28-30` | FK + payload `DistrictExistConditionComponent` + `DistrictOpenConditionTag` + kind `Exist` + state `Closed` |
| `…/SpawnCityCenterOutcomeSubSystem.cs` `:27-29` | FK + `DistrictBuildOutcomeTag` + kind `SpawnCityCenter` (no state — outcomes do not evaluate) |

**Consumer rewrites:**

| File | Sites |
|---|---|
| `…/DistrictSingleOpenConditionEvaluatorSubSystem.cs` | `_singleConditions` (`:20`, `:27-31`) → `EntityMultiMap<DistrictOpenConditionKindComponent>` = `.With<DistrictOpenConditionTag>().AsMultiMap<…KindComponent>()`; `Evaluate` `:41-56` → `TryGetEntities(new …KindComponent { Value = SingleOpen }, out var rows)` (no rows = valid state, plain return); per row: district lookup key converts FK→Data `new DistrictTypeComponent { Value = row.Get<DistrictTypeFKComponent>().Value }`; the `Has/Set/Remove` toggle `:46-55` → change-only state write (`Closed`/`Buildable`); `_builtDistrictsByType` `:33-36` STAYS keyed on `DistrictTypeComponent` (the District table's legal self-index); header comment `:10-14` |
| `Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildListUISubSystem.cs` | `_buildable` (`:32-35`) → `EntityMultiMap<DistrictOpenStateComponent>` = `.With<DistrictOpenConditionTag>().AsMultiMap<…StateComponent>()`; `:55-57` default selection + `:67-71` list loop → `TryGetEntities(new …StateComponent { Value = Buildable }, out var rows)`; `Get<DistrictTypeComponent>` on rows (`:56`, `:69`) → `Get<DistrictTypeFKComponent>`; comment `:14` |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs` | `:86` transaction draft Set → FK |
| `…/BuildDistrictActionCancelSystem.cs` | `:60` With (transaction), `:85` Get → FK |
| `…/BuildDistrictCompletionSystem.cs` | `:42` With (transaction), `:77` Get → FK; `:83` `fact.Set(new DistrictTypeComponent { Value = type })` STAYS Data (the fact IS a district of that type) |
| `Assets/Presentation/Districts/Systems/DistrictBuildProgressViewSpawnSystem.cs` | `:55` With (transaction rows), `:85` Get → FK |
| `Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelDistrictSystem.cs` | in-progress branch ONLY: `:68` With, `:122` Get → FK; the district-FACT branch (`:63` With, `:107`) STAYS Data |

Data side — untouched: `DistrictViewSpawnSystem.cs:53/:83` (reads District FACTS),
`BuildDistrictCompletionSystem.cs:83` (writes the fact), the evaluator's `_builtDistrictsByType` map.

Comments in the same diff: `DistrictTypeComponent.cs` header (now: pure Data on district facts;
referencing rows carry the FK), `DistrictOpenConditionTag.cs`, `DistrictBuildOutcomeTag.cs`,
`DistrictExistConditionComponent.cs`, `DistrictOpenConditionEvaluatorSubSystem.cs:7/:23` (base),
`DistrictOpenConditionEvaluatorBootstrapSystem.cs:13`, `DistrictOpenConditionEvaluatorSystem.cs:14`.

DoD: `grep -rn "DistrictCanBeBuildTag\|DistrictSingleOpenConditionTag\|SpawnCityCenterOutcomeTag" Assets`
→ 0; `ecsg.py tags` → archetype/filter/toggle warnings gone (0 tag deviations if S1+S2 landed);
`ecsg.py explain DistrictTypeComponent` → carried ONLY by the District archetype; `fk_of` edge
`DistrictTypeFKComponent → DistrictTypeComponent` exists; Unity check: build window opens, the list
shows buildable districts (bootstrap evaluation), city-center outcome still spawns.

## S4a — Hex space, domain half

New file: `Assets/Domains/Map/Hex/Components/HexIdFKComponent.cs` (wraps `HexCoord Coords`).

| File | Sites | Side |
|---|---|---|
| `Assets/Domains/Map/HexResources/Systems/ClayResourceGenerationSubSystem.cs` | `:197` resource-row Set → FK; hex reads `:35/:63/:66` (chains with `HexTag`) untouched | mixed |
| `…/FishResourceGenerationSubSystem.cs` | `:184` Set → FK; `:33/:61/:64` PK untouched | mixed |
| `…/ForestResourceGenerationSubSystem.cs` | `:231` Set → FK; `:36/:73/:76` PK untouched | mixed |
| `Assets/Domains/Actions/BuildDistrictAction/Systems/BuildDistrictActionSystem.cs` | `:85` transaction Set → FK | FK |
| `…/BuildDistrictActionCancelSystem.cs` | field `:40`, chain `:59-63` `AsMultiMap` → FK | FK |
| `…/BuildDistrictCompletionSystem.cs` | `:41` With (transaction), `:76` Get → FK; `:82` `fact.Set` (the District fact references its hex) → FK | FK |
| PK — untouched | `GenerationSystem`, `Lake/Mountain/River/SeaGenerationSubSystem`, `HexPathfindingUtility.cs:79` | — |

DoD: roslyn diagnostics; `ecsg.py spaces` → Hex carriers shrink; Unity check: map generates,
resources spawn, district build completes end-to-end.

## S4b — Hex space, presentation half

| File | Sites | Side |
|---|---|---|
| `Assets/Presentation/HexResources/Helpers/ForestPlanter.cs` | `:79` view-row Set → FK; `:110` reads HEX rows (PK) untouched | mixed |
| `…/Systems/ForestSpawnSystem.cs` | field `:35`, chains `:52-59` (forest views + resource rows) → FK; `_hexSet` `:61` PK untouched; `:97` `hex.Get` PK → construct FK for lookups | mixed |
| `…/Systems/ForestDespawnSystem.cs` | field `:34`, chains `:44-51` → FK; `:62` resource-row Get → FK | FK |
| `…/Systems/HexResourcesViewSubSystem.cs` | `:31` With (resource rows) → FK | FK |
| `…/Systems/ForestHexResourceViewSubSystem.cs` | `:70` forest-row Get → FK; `_hexSet` `:39` PK untouched | mixed |
| `…/Systems/ClayHexResourceViewSubSystem.cs` | `:80` clay-row Get → FK; `:43/:110` PK untouched | mixed |
| `Assets/Presentation/Districts/Systems/DistrictViewSpawnSystem.cs` | `:52` With (District rows — carry the FK after S4a), `:79` Get → FK; `:99` view-row Set → FK; multimap `:57-59` → FK | FK |
| `…/DistrictBuildProgressViewSpawnSystem.cs` | `:54` With (transaction), `:81` Get, `:101` view Set, multimap `:59-61` → FK | FK |
| `…/DistrictBuildProgressViewDespawnSystem.cs` | fields `:29/:32`, both multimaps `:43-49` → FK | FK |
| `Assets/Presentation/HexIcons/Systems/HexIconsSpawnSystem.cs` | container-row Set (after `:80`; comments `:71-73`) → `new HexIdFKComponent { Coords = hexId.Coords }`; `_hexSet` `:77` / `:80` Get PK untouched | mixed |
| `…/HexIconsVisibilitySystem.cs` | `:44/:48` With (containers + resource rows), `:84/:87` Gets → FK | FK |
| `…/HexIconsContainerPositionSystem.cs` | `:46` With, `:61` Get → FK | FK |
| `Assets/Presentation/UI/MainHud/HexInfoPanel/Systems/HexInfoPanelResourcesSystem.cs` | `:50` With, `:80` Get (resource rows) → FK | FK |
| `…/HexInfoPanelDistrictSystem.cs` | `:63/:68` With, `:107/:119` Gets (districts + transaction) → FK | FK |
| `…/HexInfoPanelSystem.cs`, `…/HexInfoPanelHeaderSystem.cs` | `_hexSet` chains with `HexTag` | PK — untouched |
| `Assets/Presentation/UI/DistrictBuild/Systems/DistrictBuildHexResourcesUISubSystem.cs` | multimap `:45` → FK, lookups convert; `_hexSet` `:43` PK untouched | mixed |
| `Assets/Modules/UserInput/Systems/CameraMovementSystem.cs` | `:58-61` chain has `HexTag` | PK — untouched |
| comments | `DistrictViewComponent.cs:8`, `DistrictBuildProgressViewComponent.cs:8`, `HexIconContainerComponent.cs:6` (comment-only mentions → name the FK type) | — |
| `Terrain*`, `WaterViewSubSystem` | chains with `HexTag` | PK — untouched |

DoD: `spaces` → "(key-role law: clean)"; `grep -rn "AsMultiMap<HexIdComponent>" Assets` → 0;
full Unity playtest pass (map gen → select hex → build district → forest/icons/info panel).

## S5 — Sweep & closure

- Delete leftover "same component is the FK" comment clauses (`HexIdComponent.cs` header if any,
  `ActionIdComponent.cs:7` "Mirrors …" — reword to name the key-role law).
- Remove the `<!-- doc-lint: off/on -->` wrapper around the hex-space table + materializations in
  `ECS_CONVENTIONS.md` (the FK names exist now); re-run `python3 Tools/doc_lint.py` → 0 ghosts.
- Rebuild graphs (`build_graph.py`; `build_di_graph.py` only if DI wiring changed — it should NOT),
  re-run `python3 Tools/gen_index.py`.
- Final meter: `ecsg.py spaces` → clean; `ecsg.py tags` → 0 deviations; `ecsg.py tables` → every
  entry PK / FK / IDX, no conflation warnings.
- Ask the user: delete this plan file (program complete) — the record lives in commit history.

## Hard bans (every session)

- NEVER hand-write/generate Unity `.meta` (new `.cs` files are fine — Unity generates their metas;
  deleting a `.cs` removes its paired `.meta` in the same commit).
- NO scene/prefab/asset edits; no Unity builds; roslyn diagnostics is a pre-check, Unity is the authority.
- ECS writes via `Set()` only (change-only for state components); zero-allocation systems; fail-loud
  (no silent skip); no tests.
- Touch ONLY the step's listed files. `ARCHITECTURE.md` is FROZEN. A plan↔code mismatch = STOP + ask.

<!-- doc-lint: on -->
