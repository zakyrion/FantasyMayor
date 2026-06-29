---
category: A
read: reference
tags: [ui, ecs]
related:
  - "[HEX_INFO_PANEL](./HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[END_TURN](./EndTurn/END_TURN.md)"
  - "[CONTEXT_TABS](./ContextTabs/CONTEXT_TABS.md)"
  - "[GENERAL_UI_STYLE](../../../GENERAL_UI_STYLE.md)"
status: partial
---

# MainUI

The **`Presentation.UI` assembly** (`Assets/Presentation/UI/`, namespaces `Presentation.UI.*`) — the
screen-space HUD half of the presentation tier (`ARCHITECTURE.md` → Presentation Layer) and the game's
UI-window home. Was the former `MainUI` module; relocated into Presentation but keeps the `UI/MainUI` prefab
name. Currently owns three windows: the terrain-generator
menu (`MainMenu` state) and — in Gameplay — the two sub-panels of one shared **bottom panel**
(`GENERAL_UI_STYLE.md` §4): the turn corner (left) and the selection-driven hex context panel (right). Also
owns the `HexTerrainIconConfigLoaderSystem` Config Loader (terrain icon + name sprites for the panel header).

## Structure — one subfolder per window
The assembly is split into **per-window subfolders**, each mirroring the by-type layout
(`Components/ Configs/ Data/ Events/ Systems/ Views/ Prefabs/`). Assembly-level pieces (`Installer/`,
this doc, the `Presentation.UI.asmdef`) stay at the root. Windows:
- `GeneratorMenu/` — the terrain-generator screen (`HexesUI` view + `ShowHexesUISystem`).
- `HexInfoPanel/` — the selected-hex **context sub-panel** (right) of the bottom panel (+ the terrain-icon
  config). Design: `HexInfoPanel/HEX_INFO_PANEL.md`.
- `EndTurn/` — the **turn sub-panel** (left) of the bottom panel («Хід N», two AP tiles «Дії зараз» / «наст.
  хід», End Turn button); also owns the bottom-panel shell reveal. Design: `EndTurn/END_TURN.md`.
- `ContextTabs/` — the **permanent tab row** (Огляд / Будівлі / Дії) of the context sub-panel: a single-select
  `ui:Toggle` group, active-tab state, the change event, a stub availability system, **and the swap of the three
  content panes** (`OverviewPane` / `BuildingsPane` / `ActionsPane`) it drives via `SetActive`. Design:
  `ContextTabs/CONTEXT_TABS.md`.
- `ResourceBar/` — the **left-edge resource panel** (`GENERAL_UI_STYLE.md` §4 left edge): the City + Mayor
  inventory pools as a vertical scroll list (one row per resource: icon + City + Mayor). Also reveals the thin
  empty top-bar strip. Owns its own icon config (`InventoryResourceIconConfig`) whose entries define the rows.
  Design: `ResourceBar/RESOURCE_BAR.md`.
- `DistrictBuild/` — the **district-build modal overlay** opened from the HexInfoPanel «Район» build slot. The
  one exception to the shared-`PanelRenderer` rule: it is its **own UIDocument** with a higher sort order so it
  renders above and blocks the whole HUD. Has its own spawn orchestrator (not a `MainUISpawnSubSystem`). Design:
  `DistrictBuild/DISTRICT_BUILD.md`.

## UI root and the single Main UI prefab
The shared full-screen UI root is module **`MainCanvas`** (`IMainCanvasProvider.RootGO`). Under it, the whole
Main UI is **one addressable prefab `UI/MainUI`** with **one `PanelRenderer`** (Unity 6 world-space UI host)
whose UXML tree carries every window's markup. The Gameplay HUD is **one unified bottom-panel shell**
(`BottomPanel`) split into two sub-panels by a vertical divider — `TurnPanel` (left) and `ContextPanel`
(right) — NOT two floating boxes (the named anti-pattern in `GENERAL_UI_STYLE.md` §14). Each window's view is
a MonoBehaviour that references **that same `PanelRenderer`** and queries only its own elements:
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
- `HexInfoPanelSystem` is a **Reactive System** (Gameplay, anchored on the `SelectedHexChangedEvent` pulse
  raised by `UserInput.HexSelectionSystem`): it reconciles the context sub-panel **show/hide** (filled ↔ empty
  placeholder) against the current `HexSelectedComponent`. The initial empty state is seeded by the spawn
  subsystem; it does NOT show/hide the shell and does NOT fill blocks.
- `HexInfoPanelHeaderSystem` / `HexInfoPanelResourcesSystem` / `HexInfoPanelDistrictPlaceholderSystem`
  are **Reactive Systems** anchored on the **same `SelectedHexChangedEvent` pulse** — each fills its panel
  block from the current `HexSelectedComponent` (no intermediary refresh event; each gates on a real hex /
  skips gracefully when nothing is selected).
- `ResourceBarSystem` is a **Per-frame System** (Gameplay, anchored on the `ResourceBarViewComponent`
  singleton): it reveals the left resource panel + the thin top-bar strip (both spawn hidden) and fills the
  City + Mayor inventory amounts each frame. Per-frame is a deliberate override of Reactive-by-default — no
  `ResourcesChanged` pulse exists yet (justification in `ResourceBar/RESOURCE_BAR.md`). Not an event trigger.
- `EndTurnViewSystem` is a **Per-frame System** (Gameplay, anchored on the `EndTurnViewComponent` singleton):
  it reveals the whole bottom panel (it owns the shell reveal), mirrors `TurnProcessorComponent` presence into
  the Processing look, and pushes the current turn number (`TurnCountComponent`, module `Turn`) into «Хід N».
  The button emits `NextTurnEvent` from `EndTurnView` on click. See `EndTurn/END_TURN.md`.
- `ContextTabSelectionSystem` is a **Reactive System** (Gameplay, anchored on the payload-less
  `ContextTabChangedEvent`): it reconciles the view against the `ActiveContextTabComponent` world singleton (the
  view writes it on click). `ContextTabsAvailabilitySystem`
  is a **Reactive System** (anchored on the `SelectedHexChangedEvent` pulse): it reconciles per-tab
  enabled/disabled state from the current selection (currently a stub — all enabled). See
  `ContextTabs/CONTEXT_TABS.md`.
- These reactive triggers are not visible in graphify — full event flow: `ECS_REFERENCE.md`.
  Roles: `ARCHITECTURE.md` "System Taxonomy".

## Non-Obvious Invariants
- **The shared `PanelRenderer` builds its visual tree asynchronously** (unlike `UIDocument`, whose root is
  ready in `OnEnable`). Each of the four shared-host views (`HexInfoPanelView` / `EndTurnView` /
  `ContextTabsView` / `ResourceBarView`) registers `PanelRenderer.RegisterUIReloadCallback` in `OnEnable`,
  binds its elements + (re)hooks button/toggle callbacks **in that callback**, and **replays its logical
  state** there — so view methods called by the spawn subsystems before the root exists just record intent
  and apply once the panel is ready (also survives a live UI reload, which recreates the elements). The
  generator menu (`HexesUI`) is a **separate** `UIDocument` prefab and is unaffected.
- **USS `picking-mode` is not supported in this Unity version.** Raycast-transparency is applied from C#
  in each shared-host view's reload callback (`HexInfoPanelView.ConfigurePicking` / the `raycast-transparent`
  sweep) via `EnablePicking(false)` (Unity.AppUI). The `raycast-transparent` USS class is only a semantic tag
  marking which elements to disable in code — setting it in USS alone does nothing.
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
  ordered by Priority): `HexInfoPanelSpawnSubSystem` (0), `EndTurnSpawnSubSystem` (10) and
  `ContextTabsSpawnSubSystem` (20). Each subsystem instantiates nothing — it `GetComponentInChildren`s its view
  off the shared instance and publishes the view singleton. `HexInfoPanelSpawnSubSystem` sets the context to
  its empty state; `EndTurnSpawnSubSystem` leaves the whole bottom-panel shell hidden — `EndTurnViewSystem` reveals
  it on entering Gameplay; `ContextTabsSpawnSubSystem` seeds the view + active tab (Overview) as **world
  singletons** (no entity) + applies the initial highlight.

## Current State
- Generator screen: three buttons (Generate, Second Step, Generate Mesh). **Only Generate is wired to
  logic** — the other two are placeholders.
- Bottom panel: **one unified shell** with two sub-panels (UXML/USS + systems + prefab + configs), revealed in
  Gameplay by `EndTurnViewSystem`.
- Context sub-panel (HexInfoPanel): **implemented**. Fixed-height shell, permanent tab row over a filled/empty
  swap, with three tab panes inside the filled state (`OverviewPane` / `BuildingsPane` / `ActionsPane`). The
  Overview pane's «Гекс» block (icon + name + Resources) binds to real components; the «Район» + «Праця та
  виробництво» blocks are SCAFFOLD (both hidden by `HexInfoPanelDistrictPlaceholderSystem`) until gameplay
  components land; `BuildingsPane` / `ActionsPane` are empty named containers; the empty state is intentionally
  blank.
- Turn sub-panel (EndTurn): **implemented** (View/Component/Spawn/System + markup in the shared document). The
  End Turn button, **«Хід N» (live, bound to `TurnCountComponent`)**, and the **two AP tiles** («Дії зараз» /
  «наст. хід», fed by `EndTurnViewSystem` from the Mayor's live `ActionPoint` stack / `MayorAPRestoreComponent`)
  all work. See `EndTurn/END_TURN.md`.
- Resource strip (ResourceBar): **code implemented** — config + loader/component, View, Spawn subsystem,
  per-frame `ResourceBarSystem`, `TopBar` markup in the shared document + USS, DI + Boot wiring. Needs
  Unity-side authoring: the `InventoryResourceIconConfig.asset` (+ sprites) at key `"InventoryResourceIconConfig"`
  and a `ResourceBarView` MonoBehaviour on the `UI/MainUI` prefab with the shared `PanelRenderer` assigned
  (else the spawn subsystem throws). See `ResourceBar/RESOURCE_BAR.md`.
- Context tabs (ContextTabs): **implemented** — components/event/enum/View/Spawn + selection & availability
  systems, registered in `UIInstaller`, wired into Gameplay by `Boot`; the three `ui:Toggle` tabs (active via
  `:checked`) and the three content panes live in the shared document, and `SetActive` swaps both. Requires the
  `ContextTabsView` MonoBehaviour on the `UI/MainUI` prefab with its `PanelRenderer` assigned (else the spawn
  subsystem throws). Availability is a stub (all tabs enabled); `BuildingsPane` / `ActionsPane` content is
  pending. See `ContextTabs/CONTEXT_TABS.md`.

## Window Design Docs
- `HexInfoPanel/HEX_INFO_PANEL.md` — the selected-hex context sub-panel (right): blocks, the filled ↔ empty
  states, ECS bindings, block→system map, the EndTurn-owns-the-shell split.
- `EndTurn/END_TURN.md` — the turn sub-panel (left) + shell reveal: state-vs-agency exception, Ready/Processing,
  «Хід N» binding to `TurnCountComponent`, the two live AP tiles (Mayor AP), `NextTurnEvent` /
  `TurnProcessorComponent`. Implemented.
- `ContextTabs/CONTEXT_TABS.md` — the permanent context tab row (Огляд / Будівлі / Дії): `ContextTab` enum, the
  active-tab state component, the change event, the markup name-constant contract (tabs + the three content panes),
  the pane swap in `SetActive`, the selection/availability split.
- `ResourceBar/RESOURCE_BAR.md` — the left-edge resource panel (City + Mayor pools): the vertical row list,
  the config-defines-rows rule, the per-frame justification, the Table-Rule resource reads, prefab prereqs.
- `DistrictBuild/DISTRICT_BUILD.md` — the district-build modal overlay (separate UIDocument): the master-detail
  picker, the spawn + reactive systems, the open/close pulses, the real-vs-placeholder (Hybrid) split, the
  flattened `DistrictsBuildConfigComponent` data path, prefab/config prereqs.
