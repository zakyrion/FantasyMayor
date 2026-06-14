# MainUI

The **Main UI module** — the game's UI-window home. Currently owns three windows: the terrain-generator
menu (`MainMenu` state), the selection-driven hex info panel (Gameplay), and the End Turn button
(Gameplay HUD). Also owns the `HexTerrainIconConfigLoaderSystem` Config Loader (terrain icon + name
sprites for the panel header).

## Structure — one subfolder per window
The module is split into **per-window subfolders**, each mirroring the module's by-type layout
(`Components/ Configs/ Data/ Events/ Systems/ Views/ Prefabs/`). Module-level pieces (`Installer/`,
this doc, the asmdef) stay at the root. Windows:
- `GeneratorMenu/` — the terrain-generator screen (`HexesUI` view + `ShowHexesUISystem`).
- `HexInfoPanel/` — the selected-hex info panel (+ the terrain-icon config). Design: `HexInfoPanel/HEX_INFO_PANEL.md`.
- `EndTurn/` — the End Turn button. Design: `EndTurn/END_TURN.md`.

## UI root and the single Main UI prefab
The shared full-screen UI root is module **`MainCanvas`** (`IMainCanvasProvider.RootGO`). Under it, the whole
Main UI is **one addressable prefab `UI/MainUI`** with **one `UIDocument`** whose UXML tree carries every
window's markup (the hex info-panel card and the End Turn button are siblings inside one shared `Root`). Each
window's view is a MonoBehaviour that references **that same `UIDocument`** and queries only its own elements
(`HexInfoPanelView` → the panel card; `EndTurnView` → `EndTurnButton`); a view's `Show/Hide` toggles **its own
element's `display`, never the document root** — toggling the root would blank the whole Main UI.
`MainUISpawnSystem` instantiates that single prefab; the spawn subsystems do NOT instantiate — each pulls its
own view component off the instance via `GetComponentInChildren`. One addressable handle (owned by the
orchestrator) covers the entire Main UI. Adding a window = add its markup to the `UI/MainUI` document + its view
MonoBehaviour + a `MainUISpawnSubSystem` that resolves it.

## Trigger
- `HexInfoPanelSystem` is a **Per-frame System** (Gameplay, anchored on the `HexInfoPanelViewComponent`
  singleton) that watches the `SelectedHexComponent` singleton: it shows/hides the panel and raises a
  one-frame `HexInfoPanelRefreshEvent` when the selected hex changes.
- `HexInfoPanelHeaderSystem` / `HexInfoPanelResourcesSystem` / `HexInfoPanelDistrictPlaceholderSystem`
  are **Reactive Systems** anchored on that pulse (base set = the event) — each rebuilds its panel
  block from current world state.
- `EndTurnSystem` is a **Per-frame System** (Gameplay, anchored on the `EndTurnViewComponent` singleton):
  it reveals the button and mirrors `TurnProcessorComponent` presence into the Processing look. The button
  emits `NextTurnEvent` from `EndTurnView` on click. See `EndTurn/END_TURN.md`.
- These reactive triggers are not visible in graphify — full event flow: `ECS_REFERENCE.md`.
  Roles: `ARCHITECTURE.md` "System Taxonomy".

## Non-Obvious Invariants
- **USS `picking-mode` is not supported in this Unity version.** Raycast-transparency is applied from C#
  in each view's `Start()` via `EnablePicking(false)` (Unity.AppUI). The `raycast-transparent` USS class is
  only a semantic tag marking which elements to disable in code — setting it in USS alone does nothing.
- Every window marks its full-screen root `raycast-transparent` and leaves only its interactive
  sub-elements pickable (the panel card; the End Turn button). Clicks pass through the rest of the overlay
  to the map underneath.
- `ShowHexesUISystem.Update` is idempotent — calling it again does not spawn a second UI. The `MainMenu`
  state calls it on entry, then toggles visibility via `Show()` / `Hide()` (Hide on the transition to
  `MapCreation`). It owns the loaded prefab instance + addressable handle, released in `Dispose`
  (`ADDRESSABLE_PATTERNS.md`). The Generate button dispatches generation via an entity with
  `TerrainGenerationGenerateEventComponent + EventTag`.
- `MainUISpawnSystem` (**Pipeline Stage 800**, orchestrator) instantiates the single `UI/MainUI` prefab under
  the main canvas (owns the one addressable handle), then runs its **spawn subsystems** (`MainUISpawnSubSystem`,
  ordered by Priority): `HexInfoPanelSpawnSubSystem` (0) and `EndTurnSpawnSubSystem` (10). Each subsystem
  instantiates nothing — it `GetComponentInChildren`s its view off the shared instance, publishes the view
  singleton, and leaves the view hidden — the matching Gameplay per-frame system shows it.

## Current State
- Generator screen: three buttons (Generate, Second Step, Generate Mesh). **Only Generate is wired to
  logic** — the other two are placeholders.
- Hex info panel: **implemented** (systems + UXML/USS + prefab + configs). Header and Resources blocks
  bind to real components; the District block is SCAFFOLD (hidden by `HexInfoPanelDistrictPlaceholderSystem`)
  until gameplay components land.
- End Turn button: **implemented** (View/Component/Spawn/System + uxml/uss). The «Хід N» and «Дії» parts of
  the turn cluster are NOT built (no data source yet). See `EndTurn/END_TURN.md`.

## Window Design Docs
- `HexInfoPanel/HEX_INFO_PANEL.md` — the selected-hex info panel: blocks, progressive-disclosure states,
  ECS bindings, block→system map.
- `EndTurn/END_TURN.md` — the End Turn button (bottom-left): state-vs-agency exception, Ready/Processing,
  binding to Turn's `NextTurnEvent` / `TurnProcessorComponent`. Implemented.
