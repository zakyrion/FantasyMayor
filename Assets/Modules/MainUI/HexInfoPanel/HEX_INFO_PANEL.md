# Hex Info Panel

The read-only state panel for the currently selected hex. Grows by progressive disclosure.

> **Category:** B (design spec) per `DOC_STANDARD.md`. Style, tokens, component catalog, placement, and the
> construction pattern come from `GENERAL_UI_STYLE.md` (repo root) — this doc does **not** restate them.
> **Visual reference:** `design-mockups/index.html` (three states).
> **Read policy:** read when building or changing this panel.

## Purpose
One shared panel that shows everything known about the selected hex. **State only — no actions** (actions are
Mayor cards). Sections appear only when their data exists.

## Trigger
Driven by the `SelectedHexComponent` singleton entity, created / updated / removed by
`UserInput.HexSelectionSystem`. `SelectedHexComponent.Coords` is the selected hex coordinate.
- The root controller shows the panel when that entity exists, hides it when it is gone, and refreshes when
  `Coords` changes.
- This reactive trigger is not visible in graphify — full selection flow: `ECS_REFERENCE.md`.

## Blocks (in progressive-disclosure order)

### 1. Header — always present
- **Terrain icon + name:** from the terrain tag on the selected hex entity — `HexPlainTag` / `HexMountTag` /
  `HexBedhillTag` / `HexWaterTag` (data-less tags; tag *presence* is the type). Each tag maps to one
  sprite + label.
- **Coordinate:** `SelectedHexComponent.Coords`.
- **Required prerequisite:** a hex must carry exactly one terrain tag. None → throw (fail-loud). This is *not*
  an "optional block absent" case.

### 2. Resources — when the hex has ≥1 resource
- Source: the **dedicated** resource entities (`HexIdComponent + HexResourcesComponent`), matched to the
  selected hex by `HexIdComponent`. Resources are **not** stored on the hex entity.
- `HexResourcesComponent.Type` (`ResourceType`) → one chip (icon + label) per resource.
- Variable count → chips built from a pooled item template (per `GENERAL_UI_STYLE` Panel Construction).
- Hidden when no matching resource entity exists.

### 3. District — when the hex has a district — **SCAFFOLD**
- **Not implemented.** `District` / `Owner` / `Operator` / `Workforce` / `Yield` exist only at the
  `GAMEPLAY_FOUNDATION.md` level; there are no backing ECS components yet.
- Driven by a **placeholder system** that keeps the block hidden (or shows stub data) until the real
  components land. Replace it with the real subsystem when they do.

## Block → System map
One system per block (per `GENERAL_UI_STYLE` Panel Construction; roles per `ARCHITECTURE.md`
"System Taxonomy"):
- **`HexInfoPanelSpawnSubSystem`** — Main UI spawn subsystem (run by `MainUISpawnSystem`, pipeline 800):
  resolves the panel view off the shared `UI/MainUI` instance (`GetComponentInChildren`) and publishes the
  view singleton, leaving the panel hidden. Instantiates nothing — the orchestrator owns the Main UI handle.
- **`HexInfoPanelSystem`** — Per-frame System (550): watches `SelectedHexComponent`; shows/hides the
  whole panel and raises the one-frame `HexInfoPanelRefreshEvent` on selection change.
- **`HexInfoPanelHeaderSystem`** — Reactive System (560): terrain tag + coord → header block.
- **`HexInfoPanelResourcesSystem`** — Reactive System (561): resource entities → chips; toggles the
  resources block.
- **`HexInfoPanelDistrictPlaceholderSystem`** — Reactive System (562), SCAFFOLD; keeps the district
  block hidden until real components exist.

## Implementation Notes
- **One shared instance** (selection is singular), not a panel per hex. World-space per-hex badges are a
  separate concern (HexIcons).
- **Picking:** the full-screen layers (document root + `Root`) are click-through so empty-area clicks reach
  the map, but the **panel card blocks** clicks — clicking it does not select/deselect a hex behind it.
  Implemented in `HexInfoPanelView.ConfigurePicking` via `EnablePicking(false)` on those two ancestors only
  (`picking-mode` is unsupported in USS in this Unity version; the card keeps the default pickable mode).
  Relies on `HexSelectionSystem`'s `EventSystem.IsPointerOverGameObject()` guard + an EventSystem in the scene.
- **Coordinate resolution:** resolve `Coords` to the hex entity (for terrain tags) and to resource entities
  (by `HexIdComponent`). The lookup mechanism is an implementation detail.
- **USS / construction gotchas:** authored skeleton + `display` toggle; no `box-shadow` / `::before` / blur —
  see `GENERAL_UI_STYLE.md` Panel Construction + USS Mapping.

## Current State
**Implemented and wired** — code (UXML/USS + systems in `HexesUI`) AND Unity-side authoring:
`Prefabs/HexInfoPanelView.prefab` and the `HexTerrainIconConfig` asset
(`Assets/Addressables/Configs/HexIconsConfigs/`) exist and are addressable. Header + Resources bind
to existing components (`HexCore` terrain tags, `HexResources`); the District block is SCAFFOLD,
hidden by `HexInfoPanelDistrictPlaceholderSystem`, pending gameplay components. The systems are
wired into `Boot` (spawn in the pipeline, the rest in `Gameplay`) — see the Block → System map above.

`HexTerrainIconConfigLoaderSystem` (Config Loader, ConfigLoadStep) loads `HexTerrainIconConfig` →
world component. Resource sprites reuse `HexIcons.HexResourceIconConfigComponent` (made `public`).
Resource chip label = the `ResourceType` name until a localized name source exists.

Editor preview: open `Prefabs/HexInfoPanel.uxml` in **UI Builder** to see the panel populated — a UIDocument
does **not** render in the Scene/Game view in edit mode, so opening the prefab shows nothing. The header text
and the two chips authored in the UXML are **preview-only**; at runtime the systems overwrite the header and
`HexInfoPanelView` strips the sample chips before rebuilding from real data. The panel root element is named
`HexInfoPanelView` (must match `HexInfoPanelView.PanelName`).

Verify in editor (not provable from code): the 4 terrain entries in `HexTerrainIconConfig` and the
resource sprites in `HexResourceIconConfig` are populated — empty entries render as skipped chips/icons.

Visual target: `design-mockups/index.html`.
