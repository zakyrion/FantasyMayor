# Hex Info Panel — Context Sub-Panel

The read-only **CONTEXT sub-panel** (right) of the shared bottom panel: everything known about the currently
selected hex. Grows by progressive disclosure.

> **Category:** B (design spec) per `DOC_STANDARD.md`. Style, tokens, component catalog, placement, and the
> construction pattern come from `GENERAL_UI_STYLE.md` (repo root) — this doc does **not** restate them.
> **Visual reference:** `design-mockups/index.html` (State A empty, State B filled).
> **Read policy:** read when building or changing this panel.

## Purpose
The right half of the always-present bottom panel (`GENERAL_UI_STYLE.md` §4). Shows everything known about the
selected hex. **State only — no actions** (actions are Mayor cards). The bottom-panel SHELL is permanent; only
this sub-panel's CONTENT is contextual — it swaps between the **filled** blocks (a hex is selected) and an
**empty placeholder** (nothing selected). The left sub-panel (turn + End Turn) is a separate window — see
`EndTurn/END_TURN.md`.

## Ownership split (read this first)
- The bottom-panel **shell** (`BottomPanel`) is revealed/hidden by **`EndTurnView` / `EndTurnSystem`** — the
  turn corner is its always-present part. This panel does **not** touch the shell.
- This panel owns only the **context content swap**: `HexInfoPanelView.ShowSelection()` (filled) ↔
  `ShowEmpty()` (placeholder). Both toggle `display` on the `ContextFilled` / `ContextEmpty` containers, never
  the document root or the shell.

## Trigger
Driven by the `SelectedHexComponent` singleton entity, created / updated / removed by
`UserInput.HexSelectionSystem`. `SelectedHexComponent.Coords` is the selected hex coordinate.
- `HexInfoPanelSystem` swaps to the filled content when that entity exists over a real hex, swaps back to the
  empty placeholder when it is gone (or the coordinate carries no hex), and raises `HexInfoPanelRefreshEvent`
  when `Coords` changes.
- This reactive trigger is not visible in graphify — full selection flow: `ECS_REFERENCE.md`.

## Blocks (in progressive-disclosure order)

### Filled state — a hex is selected (`ContextFilled`)

#### 1. Header — always present (when filled)
- **Terrain icon + name:** from the terrain tag on the selected hex entity — `HexPlainTag` / `HexMountTag` /
  `HexBedhillTag` / `HexWaterTag` (data-less tags; tag *presence* is the type). Each tag maps to one
  sprite + label.
- **Coordinate:** `SelectedHexComponent.Coords`.
- **District tabs — SCAFFOLD.** `Огляд / Будівлі / Вихід` live in the header row. They are **visual chrome
  only** — the full-window drill-downs do not exist yet, no system wires them, and they are not pickable. They
  mark where district navigation will live.
- **Required prerequisite:** a hex must carry exactly one terrain tag. None → throw (fail-loud). This is *not*
  an "optional block absent" case.

#### 2. Resources — when the hex has ≥1 resource (`ctx-a`)
- Source: the **dedicated** resource entities (`HexIdComponent + HexResourcesComponent`), matched to the
  selected hex by `HexIdComponent`. Resources are **not** stored on the hex entity.
- `HexResourcesComponent.Type` (`ResourceType`) → one chip (icon + label) per resource.
- Variable count → chips built from a pooled item template (per `GENERAL_UI_STYLE` Panel Construction).
- Hidden when no matching resource entity exists.

#### 3. District + Yield — when the hex has a district — **SCAFFOLD**
- Two blocks that share one backing concern: the **District** kvgrid (`ctx-a`: Спеціалізація / Праця / Власник
  / Оператор) and the **`Вихід цього ходу`** yield split-bar (`ctx-b`: City / Owner / Operator).
- **Not implemented.** `District` / `Owner` / `Operator` / `Workforce` / `Yield` exist only at the
  `GAMEPLAY_FOUNDATION.md` level; there are no backing ECS components yet.
- Driven by **one placeholder system** that keeps **both** blocks hidden (`SetDistrictVisible(false)` toggles
  district + yield together) until the real components land. Replace it with the real subsystem when they do.
- The example data authored in the UXML (Лісництво, 75%, +3/+3/+2, …) is **editor-preview only**.

### Empty state — nothing selected (`ContextEmpty`)
- **Intentionally blank** — no tabs, no hint text. The `ContextEmpty` container exists only as the swap target;
  when nothing is selected the context sub-panel is empty dark space. The bottom-panel shell itself stays
  permanent (the turn corner is always present); only this content is contextual.

## Block → System map
One system per block (per `GENERAL_UI_STYLE` Panel Construction; roles per `ARCHITECTURE.md`
"System Taxonomy"):
- **`HexInfoPanelSpawnSubSystem`** — Main UI spawn subsystem (run by `MainUISpawnSystem`, pipeline 800):
  resolves the panel view off the shared `UI/MainUI` instance (`GetComponentInChildren`), publishes the view
  singleton, and sets the context to its **empty** state. Instantiates nothing; does not touch the shell.
- **`HexInfoPanelSystem`** — Per-frame System (550): watches `SelectedHexComponent`; swaps the context content
  (`ShowSelection` / `ShowEmpty`) and raises the one-frame `HexInfoPanelRefreshEvent` on selection change. Does
  **not** show/hide the shell.
- **`HexInfoPanelHeaderSystem`** — Reactive System (560): terrain tag + coord → header block.
- **`HexInfoPanelResourcesSystem`** — Reactive System (561): resource entities → chips; toggles the
  resources block.
- **`HexInfoPanelDistrictPlaceholderSystem`** — Reactive System (562), SCAFFOLD; keeps the district kvgrid
  **and** the yield split hidden until real components exist.

## Implementation Notes
- **One shared instance** (selection is singular), not a panel per hex. World-space per-hex badges are a
  separate concern (HexIcons).
- **One unified bottom-panel shell.** `Prefabs/HexInfoPanel.uxml` is the single Main UI `UIDocument`. Its
  bottom panel (`BottomPanel`) is **one shell with two sub-panels** divided by a vertical divider: the turn
  sub-panel (`TurnPanel`, owned by `EndTurnView`) and this context sub-panel (`ContextPanel`). They are NOT two
  floating cards (that is the named anti-pattern in `GENERAL_UI_STYLE.md` §14).
- **`Show/Hide` semantics.** This view has no whole-panel `Show/Hide`; it exposes `ShowSelection()` /
  `ShowEmpty()` that swap `ContextFilled` ↔ `ContextEmpty`. The shell is revealed by `EndTurnView`.
- **Picking:** the full-screen layers (document root + `Root`) are click-through so empty-area clicks reach the
  map, but the **bottom panel blocks** clicks across its whole (full-width) area — clicking it does not
  select/deselect a hex behind it. Implemented in `HexInfoPanelView.ConfigurePicking` via `EnablePicking(false)`
  on those two ancestors only (`picking-mode` is unsupported in USS in this Unity version; the panel keeps the
  default pickable mode). Relies on `HexSelectionSystem`'s `EventSystem.IsPointerOverGameObject()` guard + an
  EventSystem in the scene.
- **Coordinate resolution:** resolve `Coords` to the hex entity (for terrain tags) and to resource entities
  (by `HexIdComponent`). The lookup mechanism is an implementation detail.
- **USS / construction gotchas:** authored skeleton + `display` toggle; no `box-shadow` / `::before` / blur /
  gradient — see `GENERAL_UI_STYLE.md` Panel Construction + USS Mapping. Sizes are scaled +50% from the mockup
  base.

## Current State
**Implemented and wired** — code (UXML/USS + systems in `MainUI`) AND Unity-side authoring: the single
`UI/MainUI` prefab (`Prefabs/MainUI.prefab`, document `Prefabs/HexInfoPanel.uxml`) and the `HexTerrainIconConfig`
asset (`Assets/Addressables/Configs/HexIconsConfigs/`) exist and are addressable. Header + Resources bind to
existing components (`HexCore` terrain tags, `HexResources`); the District + Yield blocks are SCAFFOLD, hidden by
`HexInfoPanelDistrictPlaceholderSystem`, pending gameplay components; the empty state is intentionally blank. The
systems are wired into `Boot` (spawn in the pipeline, the rest in `Gameplay`) — see the
Block → System map above.

`HexTerrainIconConfigLoaderSystem` (Config Loader, ConfigLoadStep) loads `HexTerrainIconConfig` → world
component. Resource sprites reuse `HexIcons.HexResourceIconConfigComponent` (made `public`). Resource chip
label = the `ResourceType` name until a localized name source exists.

Editor preview: open `Prefabs/HexInfoPanel.uxml` in **UI Builder** to see the panel populated — a UIDocument
does **not** render in the Scene/Game view in edit mode, so opening the prefab shows nothing. The header text,
the two chips, the District kvgrid, and the Yield split are **preview-only**; at runtime the systems overwrite
the header, `HexInfoPanelView` strips the sample chips before rebuilding from real data, and the placeholder
system hides the District/Yield scaffold. The filled-context root element is named `ContextFilled` and the
empty one `ContextEmpty` (must match `HexInfoPanelView`'s constants).

Verify in editor (not provable from code): the 4 terrain entries in `HexTerrainIconConfig` and the resource
sprites in `HexResourceIconConfig` are populated — empty entries render as skipped chips/icons.

Visual target: `design-mockups/index.html`.
