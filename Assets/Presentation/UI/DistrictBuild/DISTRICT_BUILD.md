---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[HEX_INFO_PANEL](../HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[ECONOMY](../../../Domains/Economy/ECONOMY.md)"
status: partial
---

# DistrictBuild

The district-build **modal overlay** (`Assets/Presentation/UI/DistrictBuild/`, namespaces
`Presentation.UI.DistrictBuild.*`): a master-detail picker the player opens from the HexInfoPanel «Район»
build slot to choose a district to build on the selected hex. Design source: `design-mockups/FantasyMayor-DistrictBuild.html`
(the `.pickwrap` / `.picker` state).

## Why a separate UIDocument (not part of the shared Main UI)
Unlike the HUD windows (`MAIN_UI.md` — one shared `PanelRenderer`), this overlay is its **own UIDocument**,
authored with a **higher `PanelSettings` sort order** so it renders above the whole HUD. Its full-screen root +
scrim are pickable by default, so the document blocks input to everything beneath it — a real modal — without
the per-element `EnablePicking` sweep the shared HUD uses. Toggling this document's own root `display` is safe
(it is not shared, so it never blanks the HUD).

## Spawn + lifecycle
- `DistrictBuildUISpawnSystem` — **world-init Pipeline Stage 810** (`IPrioritizedUniTaskSystem<MapGenerationStep>`,
  auto-collected by the generation pipeline, **no Boot wiring**). Mirrors the loading half of `MainUISpawnSystem`:
  instantiates the addressable prefab `UI/DistrictBuildAction` under `IMainCanvasProvider.RootGO`, owns the
  addressable handle in the **world component** `DistrictBuildUIRootComponent` (Box<GameObject>), resolves
  `DistrictBuildUIView` off the instance, publishes the `DistrictBuildUIViewComponent` singleton entity
  (+`UITag`), and leaves the overlay **hidden**.
- `DistrictBuildUISystem` — **Gameplay per-frame system** anchored on the `DistrictBuildUIViewComponent`
  singleton (Priority 565; wired into Gameplay by `Boot`). It coalesces the two one-frame pulses for this one
  window: `DistrictBuildRequestedEvent` (open) and `DistrictBuildClosedEvent` (close). Per-frame (not reactive)
  is deliberate — one `AEntitySetSystem` cannot anchor on two event sets, and a singleton-anchored per-frame
  read is the established override (see `ResourceBar` / `EndTurn`).

## Open / close flow
- **Open:** `HexInfoPanelView`'s build slot raises payload-less `DistrictBuildRequestedEvent`. On the next tick
  `DistrictBuildUISystem` reads the current `HexSelectedComponent`, resolves the hex's `HexTypeComponent`
  (same scan as `HexInfoPanelHeaderSystem`), reads the `DistrictsBuildConfigComponent` catalogue + both payers'
  resource stockpiles + the Mayor's AP, pushes them via `view.SetContext(...)`, and shows the overlay. The
  window is modal, so the selection cannot change while it is open — it fills once on open.
- **Close:** the «X», the scrim, and the «ЗБУДУВАТИ» button each raise `DistrictBuildClosedEvent` from the view;
  the system hides the overlay. Build itself is a **dormant later slice** — «ЗБУДУВАТИ» only closes for now.

## What is real vs placeholder (Hybrid)
The domain models only part of the screen; the rest is **marked static placeholder** until it lands.
- **Real (bound):** district list from `DistrictsBuildConfig` (currently only `CityCenter` / `Farm` are authored
  in `DistrictType`; `Unknown` is skipped); per-district **AP cost** (`ActionPointsRequired`); **resource cost
  rows** (`DistrictPrices`) with the active payer's **stockpile** as the "have" column + shortfall marking;
  **buildability** — terrain **and** the hex-resource gate (`DistrictBuildingConfig.CanBuildOn` = `IsTerrainAllowed`
  + `IsResourceSatisfied`): a district either requires the selected hex to carry its one `RequiredHexResourceType`
  **or** to be empty (`NeedEmptyHexResourcesToBuild`) — mutually exclusive; the ВИМОГИ block lists each *set*
  dimension on its own ✓/✕ line, so the failing gate (e.g. a terrain blacklist that also covers the resource's
  home terrain) is always visible; **payer toggle**
  Мер/Місто (Mayor/City) switching the affordability column; the Mayor's current **AP**.
- **Placeholder (no model yet):** the whole **ДІЇ / ЕФЕКТ** block — capacity, the fixed-step action, the
  City/Owner/Operator yield-split table, upkeep — plus the «Селяни» (no peasant resource) and «Золото» (no gold
  resource) rows, district descriptions, and the 3 screenshot districts that have no `DistrictType`. Rendered as
  a single marked note in the detail pane.

## View composition: authored UXML + per-section binders
The design is **authored in UXML/USS**, not built in C#. `Prefabs/DistrictBuildAction.uxml` carries the
modal chrome **and the detail skeleton** (the section headers, the two payer segments, the AP row, the
ДІЇ placeholder and the confirm button) with named anchors; the three **repeating** rows are separate
**item templates** under `Prefabs/Templates/` — `DistrictRow.uxml`, `ReqLine.uxml`, `CostRow.uxml` —
cloned at runtime (variable-count content can't be authored statically). Each template `<Style>`-references
the shared `.uss`, so it renders styled when opened standalone in UI Builder.

`DistrictBuildUIView` is now a thin **coordinator**: it owns the pushed data + the selection/payer state,
exposes a read-only `IDistrictBuildData` surface (selected hex type, Mayor AP, hex resources, the
active-payer stockpile, the `IsAvailable` gate, the `CostFor` join), and delegates rendering to flat
per-section binders in `Views/Binders/`:
- `DistrictListBinder` — clones a `DistrictRow` per roster district (icon / name / availability / AP pill);
  a row click reports its index, the view re-binds list + detail.
- `DistrictRequirementsBinder` — clones a `ReqLine` per **active** requirement dimension (✓/✕ from the
  `CanBuildOn` predicates; unset dimensions render nothing).
- `DistrictCostBinder` — binds the static AP row + clones a `CostRow` per resource price vs the active
  payer's stockpile (shortfall marking); re-binds on payer toggle.
- `DistrictPayerBinder` — toggles the two static Мер/Місто segments; a click re-binds the cost "have" column.
- `DistrictActionsBinder` — **scaffold only** (the ДІЇ/ЕФЕКТ model is unbuilt; this is the seam where
  per-action rows clone later — `Bind()` is a deliberate no-op for now).

Binders are (re)constructed in the PanelRenderer reload callback (the tree rebuilds asynchronously) and
re-bound on open / selection / payer toggle. The Ukrainian label + emoji maps live in the stateless
`DistrictBuildLabels` helper, shared by the binders. The view stays the **single source of state** and of
the authoritative gate, so the per-row ✓/✕ and the overall availability can never diverge.

## Data path: the district catalogue (zero-allocation)
`DistrictBuildUISystem` reads the **world component** `DistrictsBuildConfigComponent`, published at
config-load by `DistrictsBuildConfigLoaderSystem` (Economy/District; see `ECONOMY.md`). The component carries a
**reference** to the `DistrictsBuildConfig` SO — no copy/flatten (the SO already holds the data; the loader
keeps its addressable Box alive and releases it on teardown). The system itself **allocates nothing**: it passes
the catalogue reference to the view and pushes each payer's AP + per-type stockpile amounts one method call at a
time (`SetMayorResource` / `SetCityResource`, the same zero-alloc read path as `ResourceBarSystem`). It likewise
pushes the **selected hex's HexResources** one at a time (`AddHexResource`), read from the `HexIdComponent`-keyed
`EntityMultiMap` over the dedicated `HexResourcesComponent` entities — the view needs them to evaluate the
resource / empty-hex gate. The view — a MonoBehaviour, exempt from the system no-managed-collection rule — records
amounts + hex resources in its own pre-allocated pools and passes them to `CanBuildOn` as a `ReadOnlySpan<>`; the
section binders then read the SO's `List<>` fields and these pools through the view's `IDistrictBuildData` surface
when rendering (see *View composition*).

## Block → producer map
| Detail block | Source |
|---|---|
| District list (name / availability / AP) | `DistrictsBuildConfig.Districts` (`DistrictBuildingConfig[]`) + `CanBuildOn(hexType, hexResources)` |
| ВИМОГИ — one line per ACTIVE requirement | each *set* dimension renders its own ✓/✕ from the `CanBuildOn` predicates: `ImpossibleToBuildTypes` (blacklist) / `HexTypesRequirement` (whitelist) vs the selected `HexTypeComponent`; resource line = `RequiredHexResourceType` / `NeedEmptyHexResourcesToBuild` vs the hex's `HexResourceComponent` set. Unset dimensions render nothing |
| ПЛАТНИК (Мер / Місто) | view-local toggle; affordability from the chosen payer's pushed stockpile |
| БУДІВНИЦТВО — AP | `DistrictBuildingConfig.ActionPointsRequired` + Mayor `MayorAPComponent` |
| БУДІВНИЦТВО — resource rows | `DistrictBuildingConfig.DistrictPrices` (required) vs payer `ResourceComponent` (have) |
| ДІЇ / ЕФЕКТ | **placeholder** (capacity / actions / yield split / upkeep — unmodelled) |

## Prefab prerequisites (Unity-side, authored by the user)
- Addressable prefab at key **`UI/DistrictBuildAction`** carrying the overlay UXML/USS on a `PanelRenderer`,
  with a **`DistrictBuildUIView`** MonoBehaviour whose `PanelRenderer` field is assigned (else the spawn
  system throws), and a `PanelSettings` whose **sort order is higher than the Main UI's**.
- On that same `DistrictBuildUIView`, assign the **three item-template `VisualTreeAsset` fields** —
  `DistrictRow.uxml`, `ReqLine.uxml`, `CostRow.uxml` (direct asset references, no addressable keys). If any is
  unassigned the view throws on first reload (fail-loud). Let Unity import the new `.uxml` files first so it
  generates their `.meta`.
- Addressable **`DistrictsBuildConfig`** SO (the district catalogue) — else `DistrictsBuildConfigLoaderSystem`
  throws at config-load.

## Current State
Code-complete (authored overlay UXML/USS + the three item templates, the coordinator view + per-section binders,
spawn + reactive systems, the District config loader/component, DI + Boot wiring). Needs the Unity-side prefab +
config authoring above — including the new step of assigning the three template `VisualTreeAsset` fields. The
build action is dormant («ЗБУДУВАТИ» closes only); the ДІЇ/ЕФЕКТ block is a binder scaffold (no model yet).
