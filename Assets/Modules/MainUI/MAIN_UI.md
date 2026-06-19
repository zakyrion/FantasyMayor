---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[HEX_INFO_PANEL](./HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[END_TURN](./EndTurn/END_TURN.md)"
  - "[GENERAL_UI_STYLE](../../../GENERAL_UI_STYLE.md)"
status: partial
---

# MainUI

The **Main UI module** — the game's UI-window home. Currently owns three windows: the terrain-generator
menu (`MainMenu` state) and — in Gameplay — the two sub-panels of one shared **bottom panel**
(`GENERAL_UI_STYLE.md` §4): the turn corner (left) and the selection-driven hex context panel (right). Also
owns the `HexTerrainIconConfigLoaderSystem` Config Loader (terrain icon + name sprites for the panel header).

## Structure — one subfolder per window
The module is split into **per-window subfolders**, each mirroring the module's by-type layout
(`Components/ Configs/ Data/ Events/ Systems/ Views/ Prefabs/`). Module-level pieces (`Installer/`,
this doc, the asmdef) stay at the root. Windows:
- `GeneratorMenu/` — the terrain-generator screen (`HexesUI` view + `ShowHexesUISystem`).
- `HexInfoPanel/` — the selected-hex **context sub-panel** (right) of the bottom panel (+ the terrain-icon
  config). Design: `HexInfoPanel/HEX_INFO_PANEL.md`.
- `EndTurn/` — the **turn sub-panel** (left) of the bottom panel (turn number «Хід N», «Дії» placeholder, End
  Turn button); also owns the bottom-panel shell reveal. Design: `EndTurn/END_TURN.md`.

## UI root and the single Main UI prefab
The shared full-screen UI root is module **`MainCanvas`** (`IMainCanvasProvider.RootGO`). Under it, the whole
Main UI is **one addressable prefab `UI/MainUI`** with **one `UIDocument`** whose UXML tree carries every
window's markup. The Gameplay HUD is **one unified bottom-panel shell** (`BottomPanel`) split into two
sub-panels by a vertical divider — `TurnPanel` (left) and `ContextPanel` (right) — NOT two floating cards
(the named anti-pattern in `GENERAL_UI_STYLE.md` §14). Each window's view is a MonoBehaviour that references
**that same `UIDocument`** and queries only its own elements:
- `EndTurnView` owns the **shell**: `Show/Hide` toggle the `BottomPanel` element's `display` (the turn corner
  is its always-present part).
- `HexInfoPanelView` owns the **context content**: `ShowSelection` / `ShowEmpty` swap `ContextFilled` ↔
  `ContextEmpty` inside the shell.

A view never toggles the document root — that would blank the whole Main UI. `MainUISpawnSystem` instantiates
that single prefab; the spawn subsystems do NOT instantiate — each pulls its own view component off the instance
via `GetComponentInChildren`. One addressable handle (owned by the orchestrator) covers the entire Main UI.
Adding a window = add its markup to the `UI/MainUI` document + its view MonoBehaviour + a `MainUISpawnSubSystem`
that resolves it.

## Trigger
- `HexInfoPanelSystem` is a **Per-frame System** (Gameplay, anchored on the `HexInfoPanelViewComponent`
  singleton) that watches the `SelectedHexComponent` singleton: it swaps the context sub-panel content
  (filled ↔ empty placeholder) and raises a one-frame `HexInfoPanelRefreshEvent` when the selected hex
  changes. It does NOT show/hide the shell.
- `HexInfoPanelHeaderSystem` / `HexInfoPanelResourcesSystem` / `HexInfoPanelDistrictPlaceholderSystem`
  are **Reactive Systems** anchored on that pulse (base set = the event) — each rebuilds its panel
  block from current world state.
- `EndTurnSystem` is a **Per-frame System** (Gameplay, anchored on the `EndTurnViewComponent` singleton):
  it reveals the whole bottom panel (it owns the shell reveal), mirrors `TurnProcessorComponent` presence into
  the Processing look, and pushes the current turn number (`TurnCountComponent`, module `Turn`) into «Хід N».
  The button emits `NextTurnEvent` from `EndTurnView` on click. See `EndTurn/END_TURN.md`.
- These reactive triggers are not visible in graphify — full event flow: `ECS_REFERENCE.md`.
  Roles: `ARCHITECTURE.md` "System Taxonomy".

## Non-Obvious Invariants
- **USS `picking-mode` is not supported in this Unity version.** Raycast-transparency is applied from C#
  in each view's `Start()` via `EnablePicking(false)` (Unity.AppUI). The `raycast-transparent` USS class is
  only a semantic tag marking which elements to disable in code — setting it in USS alone does nothing.
- The full-screen `Root` is marked `raycast-transparent` and only the interactive sub-elements stay pickable
  (the bottom panel itself; the End Turn button). Clicks pass through the rest of the overlay to the map
  underneath — but the full-width bottom panel blocks clicks across the whole bottom strip by design.
- `ShowHexesUISystem.Update` is idempotent — calling it again does not spawn a second UI. The `MainMenu`
  state calls it on entry, then toggles visibility via `Show()` / `Hide()` (Hide on the transition to
  `MapCreation`). It owns the loaded prefab instance + addressable handle, released in `Dispose`
  (`ADDRESSABLE_PATTERNS.md`). The Generate button dispatches generation via an entity with
  `TerrainGenerationGenerateEventComponent + EventTag`.
- `MainUISpawnSystem` (**Pipeline Stage 800**, orchestrator) instantiates the single `UI/MainUI` prefab under
  the main canvas (owns the one addressable handle), then runs its **spawn subsystems** (`MainUISpawnSubSystem`,
  ordered by Priority): `HexInfoPanelSpawnSubSystem` (0) and `EndTurnSpawnSubSystem` (10). Each subsystem
  instantiates nothing — it `GetComponentInChildren`s its view off the shared instance and publishes the view
  singleton. `HexInfoPanelSpawnSubSystem` sets the context to its empty state; `EndTurnSpawnSubSystem` leaves
  the whole bottom-panel shell hidden — `EndTurnSystem` reveals it on entering Gameplay.

## Current State
- Generator screen: three buttons (Generate, Second Step, Generate Mesh). **Only Generate is wired to
  logic** — the other two are placeholders.
- Bottom panel: **one unified shell** with two sub-panels (UXML/USS + systems + prefab + configs), revealed in
  Gameplay by `EndTurnSystem`.
- Context sub-panel (HexInfoPanel): **implemented**. Header and Resources blocks bind to real components; the
  District kvgrid + the «Вихід цього ходу» yield split are SCAFFOLD (both hidden by
  `HexInfoPanelDistrictPlaceholderSystem`) until gameplay components land; the empty state is intentionally
  blank.
- Turn sub-panel (EndTurn): **implemented** (View/Component/Spawn/System + markup in the shared document). The
  End Turn button and **«Хід N» (live, bound to `TurnCountComponent`)** work; «Дії» is a visible placeholder
  (dashes) until the AP model lands. See `EndTurn/END_TURN.md`.

## Window Design Docs
- `HexInfoPanel/HEX_INFO_PANEL.md` — the selected-hex context sub-panel (right): blocks, the filled ↔ empty
  states, ECS bindings, block→system map, the EndTurn-owns-the-shell split.
- `EndTurn/END_TURN.md` — the turn sub-panel (left) + shell reveal: state-vs-agency exception, Ready/Processing,
  «Хід N» binding to `TurnCountComponent`, «Дії» placeholder, `NextTurnEvent` / `TurnProcessorComponent`.
  Implemented.
