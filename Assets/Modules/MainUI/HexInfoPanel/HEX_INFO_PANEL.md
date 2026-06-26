---
category: B
read: reference
tags: [ui, hex]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[END_TURN](../EndTurn/END_TURN.md)"
  - "[CONTEXT_TABS](../ContextTabs/CONTEXT_TABS.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
---

# Hex Info Panel — Context Sub-Panel

The read-only **CONTEXT sub-panel** (right) of the shared bottom panel: everything known about the currently
selected hex. Grows by progressive disclosure.

> **Category:** B (design spec) per `DOC_STANDARD.md`. Style, tokens, component catalog, placement, and the
> construction pattern come from `GENERAL_UI_STYLE.md` (repo root) — this doc does **not** restate them.
> **Visual reference:** `design-mockups/FantasyMayor-HUD.html` (tab row + the Overview/Buildings/Actions panes).
> **Read policy:** read when building or changing this panel.

## Purpose
The right half of the always-present bottom panel (`GENERAL_UI_STYLE.md` §4). Shows everything known about the
selected hex **and hosts that hex's actions** (e.g. district build) as buttons that open a submenu — panels are
no longer read-only (`GENERAL_UI_STYLE.md` §9). The bottom-panel SHELL is permanent; only this sub-panel's
CONTENT is contextual — it swaps between the **filled** blocks (a hex is selected) and an **empty placeholder**
(nothing selected). The left sub-panel (turn + End Turn) is a separate window — see `EndTurn/END_TURN.md`.

## Ownership split (read this first)
Three orthogonal layers, each with one owner — none overlaps:
- **Shell height + reveal** — the bottom-panel **shell** (`BottomPanel`) is revealed/hidden by **`EndTurnView` /
  `EndTurnSystem`** (the turn corner is its always-present part) and has a **fixed height** (`.bottom-panel`,
  USS) so it never grows with content. This panel does **not** touch the shell.
- **Selection swap** — this panel owns only the context content swap: `HexInfoPanelView.ShowSelection()`
  (filled) ↔ `ShowEmpty()` (placeholder). Both toggle `display` on the `ContextFilled` / `ContextEmpty`
  containers, never the document root or the shell.
- **Tab swap** — the tab row (`ContextTabs`) is a **PERMANENT** sibling **above** the filled/empty swap (visible
  even with no selection), and the three content panes inside `ContextFilled` (`OverviewPane` / `BuildingsPane` /
  `ActionsPane`) are shown one-at-a-time by **`ContextTabsView`**, NOT by this panel. See
  `ContextTabs/CONTEXT_TABS.md`.

## Trigger
Driven by the `HexSelectedComponent` singleton entity, created / updated / removed by
`UserInput.HexSelectionSystem`, which raises a payload-less `SelectedHexChangedEvent` on every mutation.
`HexSelectedComponent.Coords` is the selected hex coordinate.
- **All four panel systems are reactive on `SelectedHexChangedEvent`** (no per-frame polling) and reconcile
  against the current `HexSelectedComponent`. There is **no intermediary refresh event** — each reads the
  selection itself:
  - `HexInfoPanelSystem` owns **show/hide** (`ShowSelection` for a real hex, else `ShowEmpty`).
  - the block systems (Header / Resources / District) **fill** their block, each gating on a real hex /
    skipping gracefully when nothing (or a non-grid coord) is selected.
- The initial empty state is seeded by the spawn subsystem.
- This reactive trigger is not visible in graphify — full selection flow: `ECS_REFERENCE.md`.

## Panes & blocks

### Filled state — a hex is selected (`ContextFilled`)
Holds the **three tab panes**; `ContextTabsView` shows exactly one at a time (the tab swap is its concern, not
this panel's — see `ContextTabs/CONTEXT_TABS.md`):
- **`OverviewPane`** — the rich block row below; the only pane with real bindings today.
- **`BuildingsPane`** / **`ActionsPane`** — **empty named containers**, content lands later (out of scope now).

The `OverviewPane` is a row of discrete blocks (mockup `.blocks`):

#### 1. Block «Гекс» — hex identity + resources
- **Terrain icon + name** (`HexHead`: `HeaderIcon` + `HeaderTitle`): from `HexTypeComponent.Type` on the
  selected hex entity (`HexType`: Plain / Mount / Bedhill / Water). Each type maps to one sprite + label.
- **No coordinate.** The design dropped it; `SetHeader(icon, title)` has no coord argument.
  `HexSelectedComponent.Coords` is still read by the system to resolve the hex, just not displayed.
- **Empty selection is valid:** a non-grid coord resolves to no hex → the header is skipped (the panel
  shows empty), not an error.
- **Resources** (`ResourcesSection` / `ResourcesContainer`): the **dedicated** resource entities
  (`HexIdComponent + HexResourcesComponent`), matched by `HexIdComponent` (resources are **not** on the hex
  entity). `HexResourcesComponent.Type` → one chip (icon + label) per resource, built from a pooled item
  template (`GENERAL_UI_STYLE` Panel Construction). Hidden when no matching resource entity exists.

#### 2. Block «Район» + Block «Праця та виробництво» — **SCAFFOLD**
- Two blocks that share one backing concern: **District** (`DistrictSection`: рівень / Власник / Оператор /
  Будівлі / Спеціалізація kv-list) and **Production** (`ProductionSection`: Праця labour line + a production
  table City / Owner / Operator).
- **Not implemented.** `District` / `Owner` / `Operator` / `Workforce` / `Yield` exist only at the
  `GAMEPLAY_FOUNDATION.md` level; there are no backing ECS components yet.
- Driven by **one placeholder system** that keeps **both** blocks hidden (`SetDistrictVisible(false)` toggles
  `DistrictSection` + `ProductionSection` together) until the real components land. Replace it with the real
  subsystem when they do.
- The example data authored in the UXML (Лісозаготівля, 75%, +3/+2/+1, …) is **editor-preview only**.

### Empty state — nothing selected (`ContextEmpty`)
- **Intentionally blank** — no hint text. The `ContextEmpty` container exists only as the swap target for the
  area **below the tab row**; when nothing is selected that area is empty dark space. The shell stays permanent
  (the turn corner is always present) and **the tab row stays visible** — only the area below it is contextual.

## Block → System map
One system per block (per `GENERAL_UI_STYLE` Panel Construction; roles per `ARCHITECTURE.md`
"System Taxonomy"):
- **`HexInfoPanelSpawnSubSystem`** — Main UI spawn subsystem (run by `MainUISpawnSystem`, pipeline 800):
  resolves the panel view off the shared `UI/MainUI` instance (`GetComponentInChildren`), publishes the view
  singleton, and sets the context to its **empty** state. Instantiates nothing; does not touch the shell.
- **`HexInfoPanelSystem`** — Reactive System (550) on `SelectedHexChangedEvent`: reconciles **show/hide only**
  (`ShowSelection` for a real hex via `HexExists`, else `ShowEmpty`). Does **not** show/hide the shell, does
  **not** fill blocks.
- **`HexInfoPanelHeaderSystem`** — Reactive System (560) on `SelectedHexChangedEvent`: reads
  `HexSelectedComponent.Coords`, resolves the hex terrain type → header block (icon + name, no coord). Skips
  gracefully on no selection / a non-grid coord (no throw — that is a valid empty selection).
- **`HexInfoPanelResourcesSystem`** — Reactive System (561) on `SelectedHexChangedEvent`: resource entities of
  the selected hex → chips; hides the block when none (or nothing selected).
- **`HexInfoPanelDistrictPlaceholderSystem`** — Reactive System (562) on `SelectedHexChangedEvent`, SCAFFOLD;
  keeps the **District** block (`DistrictSection`) **and** the **Production** block (`ProductionSection`) hidden
  until real components exist (`SetDistrictVisible(false)` toggles both together).

## Implementation Notes
- **One shared instance** (selection is singular), not a panel per hex. World-space per-hex badges are a
  separate concern (HexIcons).
- **One unified, fixed-height bottom-panel shell.** `Prefabs/HexInfoPanel.uxml` is the single Main UI document,
  hosted by the shared `PanelRenderer` (Unity 6 world-space UI host). Its bottom panel (`BottomPanel`) is **one
  shell with two sub-panels** divided by a vertical
  divider: the turn sub-panel (`TurnPanel`, owned by `EndTurnView`) and this context sub-panel (`ContextPanel`).
  They are NOT two floating boxes (the named anti-pattern in `GENERAL_UI_STYLE.md` §14). The shell has a **fixed
  height** (`.bottom-panel`, `height` in USS) and clips overflow, so swapping panes/selection never resizes it.
- **`Show/Hide` semantics.** This view has no whole-panel `Show/Hide`; it exposes `ShowSelection()` /
  `ShowEmpty()` that swap `ContextFilled` ↔ `ContextEmpty`. The shell is revealed by `EndTurnView`.
- **Picking:** the full-screen layers (document root + `Root`) are click-through so empty-area clicks reach the
  map, but the **bottom panel blocks** clicks across its whole (full-width) area — clicking it does not
  select/deselect a hex behind it. Implemented in `HexInfoPanelView.ConfigurePicking` via `EnablePicking(false)`
  on those two ancestors only (`picking-mode` is unsupported in USS in this Unity version; the panel keeps the
  default pickable mode). Relies on `HexSelectionSystem`'s `EventSystem.IsPointerOverGameObject()` guard + an
  EventSystem in the scene.
- **Coordinate resolution:** resolve `Coords` to the hex entity (for terrain type) and to resource entities
  (by `HexIdComponent`). The lookup mechanism is an implementation detail.
- **USS / construction gotchas:** authored skeleton + `display` toggle; no `box-shadow` / `::before` / blur /
  gradient — see `GENERAL_UI_STYLE.md` Panel Construction + USS Mapping. Sizes are scaled +50% from the mockup
  base.

## Current State
**Implemented and wired** — code (UXML/USS + systems in `MainUI`) AND Unity-side authoring: the single
`UI/MainUI` prefab (`Prefabs/MainUI.prefab`, document `Prefabs/HexInfoPanel.uxml`) and the `HexTerrainIconConfig`
asset (`Assets/Addressables/Configs/HexIconsConfigs/`) exist and are addressable. The context sub-panel is a
**fixed-height** shell with a **permanent tab row** over a filled/empty swap; the filled state holds three tab
panes (`OverviewPane` / `BuildingsPane` / `ActionsPane`), one shown at a time by `ContextTabsView`. The Overview
pane's «Гекс» block binds to existing components (`Map` `HexTypeComponent`, `HexResources`); the «Район» +
«Праця та виробництво» blocks are SCAFFOLD, hidden by `HexInfoPanelDistrictPlaceholderSystem`, pending gameplay
components; `BuildingsPane` / `ActionsPane` are **empty named containers** (content later); the empty state is
intentionally blank. The systems are wired into `Boot` (spawn in the pipeline, the rest in `Gameplay`) — see the
Block → System map above.

`HexTerrainIconConfigLoaderSystem` (Config Loader, ConfigLoadStep) loads `HexTerrainIconConfig` → world
component. Resource sprites reuse `HexIcons.HexResourceIconConfigComponent` (made `public`). Resource chip
label = the `ResourceType` name until a localized name source exists.

Editor preview: open `Prefabs/HexInfoPanel.uxml` in **UI Builder** to see the panel populated — a runtime UI
Toolkit panel does **not** render in the Scene/Game view in edit mode, so opening the prefab shows nothing. The Overview pane
is authored visible (Buildings/Actions panes + `ContextEmpty` authored `display:none`) so UI Builder previews the
filled overview. The header text, the two chips, the District kv-list, and the Production table are
**preview-only**; at runtime the systems overwrite the header, `HexInfoPanelView` strips the sample chips before
rebuilding from real data, and the placeholder system hides the District/Production scaffold. The filled-context
root element is named `ContextFilled` and the empty one `ContextEmpty`; the panes are `OverviewPane` /
`BuildingsPane` / `ActionsPane` (must match `HexInfoPanelView`'s and `ContextTabsView`'s constants).

Verify in editor (not provable from code): the 4 terrain entries in `HexTerrainIconConfig` and the resource
sprites in `HexResourceIconConfig` are populated — empty entries render as skipped chips/icons.

Visual target: `design-mockups/FantasyMayor-HUD.html`.
