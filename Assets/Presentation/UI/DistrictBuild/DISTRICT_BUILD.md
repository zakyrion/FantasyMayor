---
category: A
read: reference
tags: [ui, ecs, district]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[HEX_INFO_PANEL](../HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[ECONOMY](../../../Domains/Economy/ECONOMY.md)"
  - "[DISTRICT_OPEN_CONDITION](../../../Domains/Economy/DistrictOpenCondition/DISTRICT_OPEN_CONDITION.md)"
  - "[BUILD_DISTRICT_ACTION](../../../Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md)"
  - "[PATTERN_ORCHESTRATOR_SUBSYSTEM](../../../../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md)"
status: partial
code_refs:
  systems:          [DistrictBuildUISystem, DistrictBuildUISpawnSystem, DistrictBuildUISubSystem, DistrictBuildListUISubSystem, DistrictBuildHexResourcesUISubSystem, DistrictBuildPriceUISubSystem, DistrictBuildActionsUISubSystem]
  components:       [DistrictBuildUIViewComponent, DistrictBuildSelectionComponent]
  world_components: [DistrictBuildUIRootComponent, DistrictBuildListUIViewComponent, DistrictBuildHexResourcesUIViewComponent, DistrictBuildPriceUIViewComponent, DistrictBuildActionsUIViewComponent, DistrictBuildsConfigComponent, DistrictBuildCostsConfigComponent]
  events:           [DistrictBuildRequestedEvent, DistrictBuildUIClosedEvent, DistrictBuildSelectedDistrictEvent, DistrictBuildConfirmedEvent]
  views:            [DistrictBuildUIView, DistrictBuildListUIView, DistrictBuildHexResourcesUIView, DistrictBuildPriceUIView, DistrictBuildActionsUIView]
  configs:          [DistrictBuildsConfig, DistrictBuildConfig, DistrictBuildCostsConfig]
  enums:            [ActorType]
  tags:             [UITag, DistrictCanBeBuildTag]
---

# DistrictBuild

The district-build **modal overlay** (`Assets/Presentation/UI/DistrictBuild/`, namespaces
`Presentation.UI.DistrictBuild.*`): a master-detail picker the player opens from the HexInfoPanel «Район»
build slot to choose a district to build on the selected hex. Design source:
`design-mockups/FantasyMayor-DistrictBuild.html` (the `.pickwrap` / `.picker` state).

## Why a separate UIDocument (not part of the shared Main UI)
Unlike the HUD windows (`MAIN_UI.md` — one shared `PanelRenderer`), this overlay is its **own UIDocument**,
authored with a **higher `PanelSettings` sort order** so it renders above the whole HUD. Its full-screen root +
scrim are pickable by default, so the document blocks input to everything beneath it — a real modal. Toggling
this document's own root `display` is safe (it is not shared, so it never blanks the HUD).

## Architecture: orchestrator + per-section subsystems
The overlay is split by concern, following [PATTERN_ORCHESTRATOR_SUBSYSTEM](../../../../Patterns/PATTERN_ORCHESTRATOR_SUBSYSTEM.md):

- **`DistrictBuildUISystem`** — the Gameplay per-frame **orchestrator**, anchored on the
  `DistrictBuildUIViewComponent` singleton (wired into Gameplay by `Boot`). It owns **no section data** and
  does **not** write `DistrictBuildSelectionComponent` itself (that is `DistrictBuildListUISubSystem`'s sole
  responsibility — see Selection below): it coalesces this window's pulses (open / close / selection) and on
  each change re-runs every section subsystem's `Populate(root)` to **reconcile** (idempotent). It is
  per-frame, not reactive, because one `AEntitySetSystem` cannot anchor on several event sets — the
  established singleton-anchored override (see `ResourceBar` / `EndTurn`).
- **`DistrictBuildUISubSystem`** — the abstract section base (a plain `IDisposable`, NOT a system). Concrete
  sections, DI-collected into the orchestrator's `IReadOnlyList`: `DistrictBuildListUISubSystem`,
  `DistrictBuildHexResourcesUISubSystem` (ВИМОГИ), `DistrictBuildPriceUISubSystem` (БУДІВНИЦТВО, cost + payer),
  `DistrictBuildActionsUISubSystem` (ДІЇ — dormant scaffold).
- **Each section reads ECS directly and pushes to its own section view** — there is no shared read-model
  (the former `IDistrictBuildData` is retired). Roles/priorities/reads: `ecsg.py explain <name>`.

## Trigger
`DistrictBuildUISystem` is event-driven: it consumes `DistrictBuildRequestedEvent` (open),
`DistrictBuildUIClosedEvent` (close), and `DistrictBuildSelectedDistrictEvent` (re-select). These pulses are not
visible to `roslyn-mcp` — full producer→consumer flow is in the ecs-graph (`/ecs-graph`).

## Spawn
`DistrictBuildUISpawnSystem` — a world-init pipeline stage (auto-collected, **no Boot wiring**). It instantiates
the addressable prefab `UI/DistrictBuildAction` under `IMainCanvasProvider.RootGO`, owns the addressable handle
in the **world component** `DistrictBuildUIRootComponent` (`Box<GameObject>`), then resolves **each** section
MonoBehaviour off the instance (`GetComponentInChildren`) and publishes them: the root `DistrictBuildUIView` on
the `DistrictBuildUIViewComponent` singleton entity (+`UITag`), and each section view as its own **world
component** (`DistrictBuildListUIViewComponent`, `DistrictBuildHexResourcesUIViewComponent`,
`DistrictBuildPriceUIViewComponent`, `DistrictBuildActionsUIViewComponent`). It **fails loud** if any of the
four section views is missing from the prefab, and leaves the overlay hidden.

## Selection (ECS) and the open / select / close flow
Selection of the active district lives in ECS, not in a view. `DistrictBuildListUISubSystem` is the **sole
writer** of the selection (default-selects on open, writes it directly on row-click); the orchestrator only
reads it to reconcile the other sections:
- `DistrictBuildSelectionComponent` — the currently selected `DistrictType`, held on its own entity (tagged
  `DistrictBuildSelectionTag`), not a world component.
- `DistrictBuildSelectedDistrictEvent` (one-frame pulse, payload-less) — `DistrictBuildListUISubSystem` already
  wrote `DistrictBuildSelectionComponent` before raising it; the pulse only tells the orchestrator to
  re-reconcile.

Flow:
- **Open** — `HexInfoPanelView`'s build slot raises `DistrictBuildRequestedEvent`. The orchestrator (guarded by a
  live `HexSelectedComponent`) sets `DistrictBuildSelectionComponent` to the **default** (first buildable
  district), reconciles all sections, and shows the overlay.
- **Select** — the list view raises a local C# `SelectionChanged`; `DistrictBuildListUISubSystem` writes the
  new `DistrictBuildSelectionComponent` directly (synchronously, in the same handler) and raises
  `DistrictBuildSelectedDistrictEvent` (so the **view stays World-free**). Next tick the orchestrator reads
  the already-updated selection and reconciles all sections to it.
- **Confirm** — the «ЗБУДУВАТИ» button reads the selected hex + district from ECS (one-shot `EntitySet`s) and
  raises the payload-carrying `DistrictBuildConfirmedEvent` (`HexCoord` + `DistrictType`) +
  `DistrictBuildUIClosedEvent` from the root view; the Actions domain creates the committed build entity
  directly from the payload — no draft
  ([BUILD_DISTRICT_ACTION](../../../Domains/Actions/BuildDistrictAction/BUILD_DISTRICT_ACTION.md)); the
  orchestrator hides the overlay.
- **Dismiss** — the «X» and the scrim each raise ONLY `DistrictBuildUIClosedEvent` — there is no draft to
  discard, so dismiss is close-only. The orchestrator hides the overlay; confirm and dismiss are separate
  button handlers, so a click cannot combine both (full contract: `BUILD_DISTRICT_ACTION.md`).

## Section views + the PanelRenderer reload contract
Each section is its own MonoBehaviour on the overlay prefab, bound to the **shared** `PanelRenderer` tree:
- **`DistrictBuildUIView`** — chrome only: `Show`/`Hide`, close/scrim/confirm. Holds no section data; confirm
  reads the selected hex + district from ECS and raises the payload-carrying `DistrictBuildConfirmedEvent`
  (Actions creates the committed build entity directly — no draft) + close; fails loud if hex or district is
  missing at confirm (the «НЕДОСТУПНО» availability styling was dropped until the build slice).
- **`DistrictBuildListUIView`** — clones a `DistrictRow` per buildable district, marks the selected row, raises
  `SelectionChanged` on click.
- **`DistrictBuildHexResourcesUIView`** — the district-name header + one ✓/✕ `ReqLine` per active gate dimension.
- **`DistrictBuildPriceUIView`** — the AP row + a `CostRow` per resource price (need vs the current payer's
  stockpile, short-marked) + the Мер/Місто payer toggle; raises a local `PayerChanged`.
- **`DistrictBuildActionsUIView`** — dormant placeholder (validates its `PanelRenderer` only).

**PanelRenderer builds asynchronously**, so every section view (re)caches its elements + re-registers its
callbacks in the reload callback; push methods that arrive **before the first reload are no-ops** (the
async-panel reality, not a silent skip of a real prerequisite). Each view **fails loud in `OnEnable`** if its
`PanelRenderer` / templates are unassigned — never in the reload callback (a throw there runs inside the global
panel-update loop and would blank every UIDocument).

## Public Contract & Gotchas
- **`DistrictBuildSelectionComponent` is the single source of selection.** Every section subsystem reconciles to
  it idempotently in `Populate(root)` (called on open and on every selection change). A second reconcile is a
  no-op; there is no per-section selection state.
- **Payer is LOCAL to the Price view, not ECS.** `DistrictBuildPriceUIView.SelectedOwner` holds the current
  payer and raises `PayerChanged`; the subsystem re-renders the whole section (AP row + payer segments + cost
  rows) on every change. District selection goes through ECS; payer does not.
- **Sections self-read ECS (no shared read-model).** Each subsystem builds its own query caches: HexResources
  reads the selected hex's type + `HexResourceComponent` set; Price reads the actor stockpiles + the Mayor's AP
  (`ActionPoint` resource stack) + the cost catalogue (`DistrictBuildCost`, Economy) via
  `DistrictBuildCostsConfigComponent`; both look up the selected
  `DistrictBuildConfig` in `DistrictBuildsConfigComponent`.
- **The list is condition-driven, not roster-driven.** `DistrictBuildListUISubSystem` reads the
  `DistrictOpenCondition` entities tagged `DistrictCanBeBuildTag` (key `DistrictTypeComponent`), NOT the full
  `DistrictBuildsConfig` roster. The tag is the buildability key — see `DISTRICT_OPEN_CONDITION.md`.
- **The catalogue components carry live SO references** (`DistrictBuildsConfigComponent`,
  `DistrictBuildCostsConfigComponent`) — not flattened copies; the loaders keep their addressable Boxes
  alive. The systems allocate nothing — push to views one value at a time (the `ResourceBarSystem` path).

## What is real vs placeholder (Hybrid)
- **Real (bound):** the buildable district list (icon / name); per-district **AP cost** + **resource cost rows**
  vs the active payer's stockpile (shortfall-marked); **buildability requirements** — terrain + the hex-resource
  gate (`DistrictBuildConfig.CanBuildOn`: a district requires the hex to carry its one
  `RequiredHexResourceType` **or** to be empty (`NeedEmptyHexResourcesToBuild`) — the ВИМОГИ block lists each set
  dimension on its own ✓/✕ line); the **payer toggle** Мер/Місто; the Mayor's current AP.
- **Placeholder (no model yet):** the whole **ДІЇ / ЕФЕКТ** block (capacity, the fixed-step action, the yield-split
  table, upkeep) — the Actions section is a dormant scaffold.

## Prefab prerequisites (Unity-side, authored by the user)
- Addressable prefab at key **`UI/DistrictBuildAction`** carrying the overlay UXML/USS on a `PanelRenderer`, with
  a `PanelSettings` whose **sort order is higher than the Main UI's**.
- **All five section MonoBehaviours on the prefab**, each with its `PanelRenderer` field assigned (spawn fails
  loud if any of the four section views is missing): `DistrictBuildUIView` (chrome), `DistrictBuildListUIView`
  (+`DistrictRow.uxml` template), `DistrictBuildHexResourcesUIView` (+`ReqLine.uxml`), `DistrictBuildPriceUIView`
  (+`CostRow.uxml`), `DistrictBuildActionsUIView`. Each view's element-name fields default to the existing UXML
  anchors (`DistrictList`, `DetailName`/`ReqLines`, `ApRow`/`CostRows`/`PayerMayor`/`PayerCity`,
  `ActionsPlaceholder`) — wire only if the UXML renames them. Templates carry **no `<Style>`** (a relative `..`
  src breaks the importer; XML comments must avoid `--`); they inherit the host panel's stylesheet.
- Addressable **`DistrictBuildsConfig`** (class `DistrictBuildsConfig`, Economy placement) + **`DistrictBuildCostsConfig`**
  (the cost catalogue, class `DistrictBuildCostsConfig`, now in Economy) SOs — else the loaders throw at
  config-load. Let Unity import any new `.uxml` first so it generates the `.meta`.

## Current State
PARTIAL. The orchestrator + section-subsystem split is implemented: List / HexResources (ВИМОГИ) / Price
(БУДІВНИЦТВО) / Actions (dormant), each with its own section view + per-section view component, plus the ECS
selection (`DistrictBuildSelectionComponent` + `DistrictBuildSelectionRequestedEvent`). The former
coordinator+sub-view design (`IDistrictBuildData` + `Views/SubViews/`) is **retired**.

Open gaps:
- **The list is populated for `DistrictSingleOpenConditionTag`-gated districts only** — a per-turn evaluator
  now attaches/removes `DistrictCanBeBuildTag` for that kind (`DISTRICT_OPEN_CONDITION.md` → Condition
  Evaluation); `DistrictExistConditionComponent`-gated districts still have no evaluator, so their tag is
  never produced yet.
- **Confirm creates the committed build entity directly** — «ЗБУДУВАТИ» raises the payload-carrying
  `DistrictBuildConfirmedEvent`, which the Actions domain uses to create the entity — no draft
  (`BUILD_DISTRICT_ACTION.md`); the ДІЇ/ЕФЕКТ model itself (cost spend, turn-ticking, outcome) is still
  unbuilt.
- **Prefab authoring required** (the five section MonoBehaviours + templates above); the overlay spawn fails
  loud until the four section views are present.
