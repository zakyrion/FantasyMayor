---
category: A
read: reference
tags: [ui, hex]
status: partial
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[END_TURN](../EndTurn/END_TURN.md)"
  - "[CONTEXT_TABS](../ContextTabs/CONTEXT_TABS.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
code_refs:
  systems:    [HexInfoPanelSystem, HexInfoPanelHeaderSystem, HexInfoPanelResourcesSystem, HexInfoPanelDistrictSystem, HexInfoPanelSpawnSubSystem]
  components: [HexSelectedComponent, HexTypeComponent, HexIdComponent, HexInfoPanelViewComponent]
  tags:       [DistrictTag]
  events:     [SelectedHexChangedEvent, DistrictBuildRequestedEvent]
  views:      [HexInfoPanelView]
---

# Hex Info Panel — Context Sub-Panel

The read-only **CONTEXT sub-panel** (right) of the shared bottom panel: everything known about the
currently selected hex. Grows by progressive disclosure.

> Style, tokens, component catalog, placement, and the construction pattern come from
> `GENERAL_UI_STYLE.md` (repo root) — this doc does **not** restate them.
> **Visual reference:** `design-mockups/FantasyMayor-HUD.html` (tab row + the Overview/Buildings/Actions panes).
> **Read policy:** read when building or changing this panel.

## Purpose
The right half of the always-present bottom panel (`GENERAL_UI_STYLE.md` §4). Shows everything known about
the selected hex **and hosts that hex's actions** (e.g. district build) as buttons that open a submenu —
panels are no longer read-only (`GENERAL_UI_STYLE.md` §9). The SHELL is permanent; only this sub-panel's
CONTENT is contextual (filled ↔ empty). The left sub-panel (turn + End Turn) is a separate window — see
`EndTurn/END_TURN.md`.

## Ownership split (read this first)
Three orthogonal layers, each with one owner — none overlaps:
- **Shell height + reveal** — the bottom-panel **shell** (`BottomPanel`) is revealed/hidden by
  `EndTurnView`/`EndTurnViewSystem` (the turn corner is its always-present part) and has a fixed height so
  it never grows with content. This panel does **not** touch the shell.
- **Selection swap** — this panel owns only the context content swap: `ShowSelection()` (filled) ↔
  `ShowEmpty()` (placeholder), toggling `display` on `ContextFilled`/`ContextEmpty`, never the document
  root or the shell.
- **Tab swap** — the tab row (`ContextTabs`) is a **PERMANENT** sibling **above** the filled/empty swap
  (visible even with no selection); the three content panes inside `ContextFilled` are shown one-at-a-time
  by `ContextTabsView`, NOT by this panel. See `ContextTabs/CONTEXT_TABS.md`.

## Trigger
Driven by the `HexSelectedComponent` singleton entity, created / updated / removed by
`UserInput.HexSelectionSystem`, which raises a payload-less `SelectedHexChangedEvent` on every mutation.
`HexSelectedComponent.Coords` is the selected hex coordinate. **All four panel systems are reactive on
this event** (no per-frame polling) and reconcile against the current `HexSelectedComponent` — there is
**no intermediary refresh event**, each reads the selection itself (see Block → System map for the
per-system split). The initial empty state is seeded by the spawn subsystem. Not visible in roslyn-mcp —
full selection flow: the ecs-graph (`/ecs-graph`).

**Outbound event (emitted, not consumed here).** The District build slot (`DistrictBuildButton` in
`HexInfoPanelView`) raises a payload-less `DistrictBuildRequestedEvent` (+ `EventTag`) on click — the request
to open the district-build window. Payload-less by design: the consumer reads the current `HexSelectedComponent`
for the target hex. The event lives in `Presentation.UI.DistrictBuild.Events` (next to its window, not here) and
**is consumed by `DistrictBuildUISystem`**, which opens the modal overlay (see `DistrictBuild/DISTRICT_BUILD.md`).
Event registry: the ecs-graph (`/ecs-graph`).

## Panes & blocks

### Filled state — a hex is selected (`ContextFilled`)
Holds the **three tab panes**; `ContextTabsView` shows exactly one at a time (the tab swap is its concern, not
this panel's — see `ContextTabs/CONTEXT_TABS.md`):
- **`OverviewPane`** — the rich block row below; the only pane with real bindings today.
- **`BuildingsPane`** / **`ActionsPane`** — **empty named containers**, content lands later (out of scope now).

The `OverviewPane` is a row of discrete blocks (mockup `.blocks`):

#### 1. Block «Гекс» — hex identity + resources
- **Terrain icon + name** from `HexTypeComponent.Type` on the selected hex entity — each `HexType` maps
  to one sprite + label. **No coordinate** — the design dropped it; `Coords` is still read to resolve the
  hex, just not displayed. **Empty selection is valid:** a non-grid coord resolves to no hex → the header
  is skipped (panel shows empty), not an error.
- **Resources:** the **dedicated** resource entities (`HexIdComponent + HexResourcesComponent`), matched
  by `HexIdComponent` (resources are **not** on the hex entity). `HexResourcesComponent.Type` → one
  **square icon tile** per resource (icon-only — the resource name is a hover **tooltip**, not a visible
  label). Hidden when no matching resource entity exists.

#### 2. Block «Район» — three mutually-exclusive states
The District concern has **three** states, exactly one shown at a time, all driven by
`HexInfoPanelDistrictSystem` (see Block → System map). The view enforces exclusivity via
`ShowDistrictBuildPrompt()` / `ShowDistrictDetails()` / `HideDistrict()` (the old `SetDistrictVisible(bool)`
is gone):
- **Build prompt — IMPLEMENTED (no district on the hex).** `DistrictBuildSection`: «РАЙОН» header + a single
  clickable build slot (`DistrictBuildButton`: `+` circle, «Збудувати район», «обрати спеціалізацію для гекса»).
  The whole slot is the click target; clicking it raises `DistrictBuildRequestedEvent` (see `## Trigger`). This
  is the **default reachable state** today — nothing builds districts yet, so a selected grid hex always has
  none. USS note: the mockup's dashed slot border is rendered as a **solid** gold border (UI Toolkit USS has no
  dashed borders).
- **Details — SCAFFOLD (a district exists).** Two blocks sharing one backing concern: **District**
  (kv-list) and **Production** (labour + City/Owner/Operator table); they appear/disappear **together**.
  These concepts exist only at the `GAMEPLAY_FOUNDATION.md` level — no backing ECS components yet, so this
  branch is **unreachable** (nothing spawns a district). The UXML example data is **editor-preview only**;
  both blocks are authored `display:none`.
- **Hidden — nothing (or no grid hex) selected.** All three blocks hidden.

District presence is checked via the District table (key `HexIdComponent`, discriminator `DistrictTag`
from `Domains.Economy`) — see Block → System map.

### Empty state — nothing selected (`ContextEmpty`)
- **Intentionally blank** — no hint text. The `ContextEmpty` container exists only as the swap target for the
  area **below the tab row**; when nothing is selected that area is empty dark space. The shell stays permanent
  (the turn corner is always present) and **the tab row stays visible** — only the area below it is contextual.

## Block → System map
One system per block (per `GENERAL_UI_STYLE` Panel Construction). Roles/priorities: `ecsg.py explain
<System>`.
- **`HexInfoPanelSpawnSubSystem`** — Main UI spawn subsystem: resolves the panel view off the shared
  `UI/MainUI` instance, publishes the view singleton, sets the context to its **empty** state.
  Instantiates nothing; does not touch the shell.
- **`HexInfoPanelSystem`** — reconciles **show/hide only** (`ShowSelection` for a real hex via
  `HexExists`, else `ShowEmpty`). Does **not** show/hide the shell, does **not** fill blocks.
- **`HexInfoPanelHeaderSystem`** — reads `HexSelectedComponent.Coords`, resolves the hex terrain type →
  header block (icon + name, no coord). Skips gracefully on no selection / a non-grid coord (no throw —
  a valid empty selection).
- **`HexInfoPanelResourcesSystem`** — resource entities of the selected hex → chips; hides the block when
  none (or nothing selected).
- **`HexInfoPanelDistrictSystem`** — resolves the District block's state. No selection → `HideDistrict()`;
  else queries the District table (`With<HexIdComponent>().With<DistrictTag>()`, matched on the
  coordinate) → `ShowDistrictBuildPrompt()` when the hex has no district (the live path today),
  `ShowDistrictDetails()` when it has one (unreachable SCAFFOLD — nothing spawns a district yet).

## Implementation Notes
- **One shared instance** (selection is singular), not a panel per hex. World-space per-hex badges are a
  separate concern (HexIcons).
- **Picking:** the full-screen layers (document root + `Root`) are click-through so empty-area clicks
  reach the map, but the **bottom panel blocks** clicks across its whole (full-width) area — clicking it
  does not select/deselect a hex behind it. Implemented via `EnablePicking(false)` on those two ancestors
  only (`picking-mode` is unsupported in USS in this Unity version). Relies on `HexSelectionSystem`'s
  `EventSystem.IsPointerOverGameObject()` guard + an EventSystem in the scene.
- **USS / construction gotchas:** authored skeleton + `display` toggle; no `box-shadow` / `::before` /
  blur / gradient — see `GENERAL_UI_STYLE.md` Panel Construction + USS Mapping. Sizes are tuned to the
  HUD height budget (top bar + bottom panel together ≤ ~30% of screen height).

## Current State
**Implemented and wired** — code AND Unity-side authoring: the `UI/MainUI` prefab and the
`HexTerrainIconConfig` asset exist and are addressable. `HexTerrainIconConfigLoaderSystem` (Config Loader,
ConfigLoadStep) loads `HexTerrainIconConfig`; resource sprites reuse `HexIcons.HexResourceIconConfigComponent`
(made `public`). Resource chip label = the `ResourceType` name until a localized name source exists.
The District **details** blocks (`DistrictSection` + `ProductionSection`) stay unreachable SCAFFOLD — no
district-economy components yet.

**Editor preview gotcha:** a runtime UI Toolkit panel does **not** render in Scene/Game view in edit mode,
so opening `Prefabs/HexInfoPanel.uxml` in **UI Builder** is the only way to see it populated. The Overview
pane is authored visible (Buildings/Actions + `ContextEmpty` authored `display:none`) for that preview; the
header text, chips, District kv-list, and Production table shown there are **preview-only** sample data —
at runtime the systems overwrite/strip them and rebuild from real data.

Verify in editor (not provable from code): the 4 terrain entries in `HexTerrainIconConfig` and the resource
sprites in `HexResourceIconConfig` are populated — empty entries render as skipped chips/icons.

Visual target: `design-mockups/FantasyMayor-HUD.html`.
