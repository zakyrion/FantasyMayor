# HexesUI

UI Toolkit screens: the terrain-generator menu (shown by the `MainMenu` game state) and the
selection-driven hex info panel (Gameplay). Also owns the `HexTerrainIconConfigLoaderSystem`
Config Loader (terrain icon + name sprites for the panel header).

## Trigger
- `HexInfoPanelSystem` is a **Per-frame System** (Gameplay, anchored on the `HexInfoPanelViewComponent`
  singleton) that watches the `SelectedHexComponent` singleton: it shows/hides the panel and raises a
  one-frame `HexInfoPanelRefreshEvent` when the selected hex changes.
- `HexInfoPanelHeaderSystem` / `HexInfoPanelResourcesSystem` / `HexInfoPanelDistrictPlaceholderSystem`
  are **Reactive Systems** anchored on that pulse (base set = the event) — each rebuilds its panel
  block from current world state.
- These reactive triggers are not visible in graphify — full event flow: `ECS_REFERENCE.md`.
  Roles: `ARCHITECTURE.md` "System Taxonomy". Panel design + block map: `HEX_INFO_PANEL.md`.

## Non-Obvious Invariants
- **USS `picking-mode` is not supported in this Unity version.** Raycast-transparency is applied
  from C# in `HexesUI.Start()` via `EnablePicking(false)` (Unity.AppUI). The `raycast-transparent`
  USS class is only a semantic tag that marks which elements to disable in code — setting it in
  USS alone does nothing.
- Layout: every panel is raycast-transparent except the right panel, which is the only
  interactive area (holds the buttons). This lets clicks pass through the rest of the overlay
  to the map underneath.
- `ShowHexesUISystem.Update` is idempotent — calling it again does not spawn a second UI. The
  `MainMenu` state calls it on entry, then toggles visibility via `Show()` / `Hide()` (Hide on the
  transition to `MapCreation`).
- `ShowHexesUISystem` owns the loaded prefab instance and its addressable handle; both are
  released in `Dispose` (follow `ADDRESSABLE_PATTERNS.md`).
- The Generate button dispatches terrain generation by creating an entity with
  `TerrainGenerationGenerateEventComponent + EventTag`.
- `HexInfoPanelSpawnSystem` is a **Pipeline Stage** (800, last in the world-init pipeline): it loads
  the panel prefab under the main canvas, publishes the `HexInfoPanelViewComponent` singleton, owns
  the addressable handle, and leaves the panel hidden — `HexInfoPanelSystem` shows it on selection.

## Current State
- Generator screen: three buttons (Generate, Second Step, Generate Mesh). **Only Generate is wired
  to logic** — the other two are placeholders.
- Hex info panel: **implemented** (systems + UXML/USS + prefab + configs). Header and Resources
  blocks bind to real components; the District block is SCAFFOLD (hidden by
  `HexInfoPanelDistrictPlaceholderSystem`) until gameplay components land.

## Window Design Docs
- `HEX_INFO_PANEL.md` — design spec + current implementation state of the selected-hex info panel:
  blocks, progressive-disclosure states (empty / resource / district), ECS bindings, block→system
  map. Read it before changing that panel.
