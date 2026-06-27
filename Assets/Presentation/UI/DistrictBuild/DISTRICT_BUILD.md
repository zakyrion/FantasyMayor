---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[HEX_INFO_PANEL](../HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[ECONOMY](../../../Domains/Economy/ECONOMY.md)"
  - "[ECS_REFERENCE](../../../../ECS_REFERENCE.md)"
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
- `DistrictBuildActionSpawnSystem` — **world-init Pipeline Stage 810** (`IPrioritizedUniTaskSystem<MapGenerationStep>`,
  auto-collected by the generation pipeline, **no Boot wiring**). Mirrors the loading half of `MainUISpawnSystem`:
  instantiates the addressable prefab `UI/DistrictBuildAction` under `IMainCanvasProvider.RootGO`, owns the
  addressable handle in the **world component** `DistrictBuildActionRootComponent` (Box<GameObject>), resolves
  `DistrictBuildActionView` off the instance, publishes the `DistrictBuildActionViewComponent` singleton entity
  (+`UITag`), and leaves the overlay **hidden**.
- `DistrictBuildActionSystem` — **Gameplay per-frame system** anchored on the `DistrictBuildActionViewComponent`
  singleton (Priority 565; wired into Gameplay by `Boot`). It coalesces the two one-frame pulses for this one
  window: `DistrictBuildRequestedEvent` (open) and `DistrictBuildClosedEvent` (close). Per-frame (not reactive)
  is deliberate — one `AEntitySetSystem` cannot anchor on two event sets, and a singleton-anchored per-frame
  read is the established override (see `ResourceBar` / `EndTurn`).

## Open / close flow
- **Open:** `HexInfoPanelView`'s build slot raises payload-less `DistrictBuildRequestedEvent`. On the next tick
  `DistrictBuildActionSystem` reads the current `HexSelectedComponent`, resolves the hex's `HexTypeComponent`
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
  **buildability** — terrain **and** the hex-resource gate (`DistrictBuildingConfig.CanBuildOn(hexType, hexResources)`):
  a district either requires the selected hex to carry its one `RequiredResourceType` **or** to be empty
  (`NeedEmptyHexResourcesToBuild`) — the two modes are mutually exclusive; **payer toggle**
  Мер/Місто (Mayor/City) switching the affordability column; the Mayor's current **AP**.
- **Placeholder (no model yet):** the whole **ДІЇ / ЕФЕКТ** block — capacity, the fixed-step action, the
  City/Owner/Operator yield-split table, upkeep — plus the «Селяни» (no peasant resource) and «Золото» (no gold
  resource) rows, district descriptions, and the 3 screenshot districts that have no `DistrictType`. Rendered as
  a single marked note in the detail pane.

## Data path: the district catalogue (zero-allocation)
`DistrictBuildActionSystem` reads the **world component** `DistrictsBuildConfigComponent`, published at
config-load by `DistrictsBuildConfigLoaderSystem` (Economy/District; see `ECONOMY.md`). The component carries a
**reference** to the `DistrictsBuildConfig` SO — no copy/flatten (the SO already holds the data; the loader
keeps its addressable Box alive and releases it on teardown). The system itself **allocates nothing**: it passes
the catalogue reference to the view and pushes each payer's AP + per-type stockpile amounts one method call at a
time (`SetMayorResource` / `SetCityResource`, the same zero-alloc read path as `ResourceBarSystem`). It likewise
pushes the **selected hex's HexResources** one at a time (`AddHexResource`), read from the `HexIdComponent`-keyed
`EntityMultiMap` over the dedicated `HexResourcesComponent` entities — the view needs them to evaluate the
resource / empty-hex gate. The view — a MonoBehaviour, exempt from the system no-managed-collection rule — records
amounts + hex resources in its own pre-allocated pools, passes the latter to `CanBuildOn` as a `ReadOnlySpan<>`,
and reads the SO's `List<>` fields directly when rendering.

## Block → producer map
| Detail block | Source |
|---|---|
| District list (name / availability / AP) | `DistrictsBuildConfig.Districts` (`DistrictBuildingConfig[]`) + `CanBuildOn(hexType, hexResources)` |
| ВИМОГИ (terrain + resource) | terrain vs selected `HexTypeComponent`; resource line = `RequiredResourceType` / `NeedEmptyHexResourcesToBuild` vs the hex's `HexResourcesComponent` set |
| ПЛАТНИК (Мер / Місто) | view-local toggle; affordability from the chosen payer's pushed stockpile |
| БУДІВНИЦТВО — AP | `DistrictBuildingConfig.ActionPointsRequired` + Mayor `MayorAPComponent` |
| БУДІВНИЦТВО — resource rows | `DistrictBuildingConfig.DistrictPrices` (required) vs payer `ResourceComponent` (have) |
| ДІЇ / ЕФЕКТ | **placeholder** (capacity / actions / yield split / upkeep — unmodelled) |

## Prefab prerequisites (Unity-side, authored by the user)
- Addressable prefab at key **`UI/DistrictBuildAction`** carrying the overlay UXML/USS on a `PanelRenderer`,
  with a **`DistrictBuildActionView`** MonoBehaviour whose `PanelRenderer` field is assigned (else the spawn
  system throws), and a `PanelSettings` whose **sort order is higher than the Main UI's**.
- Addressable **`DistrictsBuildConfig`** SO (the district catalogue) — else `DistrictsBuildConfigLoaderSystem`
  throws at config-load.

## Current State
Code-complete (overlay UXML/USS, view, spawn + reactive systems, the District config loader/component, DI + Boot
wiring). Needs the Unity-side prefab + config authoring above. The build action is dormant («ЗБУДУВАТИ» closes
only); the ДІЇ/ЕФЕКТ block is a placeholder pending the District-operation model.
