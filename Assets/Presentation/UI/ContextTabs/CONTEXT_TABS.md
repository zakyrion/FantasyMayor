---
category: B
read: reference
tags: [ui]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[HEX_INFO_PANEL](../HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[END_TURN](../EndTurn/END_TURN.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
---

# Context Tabs — Tab Row of the Context Sub-Panel

The **tab row** (Огляд / Будівлі / Дії) of the bottom panel's context sub-panel
(`design-mockups/FantasyMayor-HUD.html`, the `.tabs` row). Window of the `Presentation.UI` assembly
(`ContextTabs/`). The three tabs are a **single-select `ui:Toggle` group** wired through **ECS events**: clicking
a tab raises a one-frame event, a system commits the new active tab into state, the view restyles **and swaps the
visible content pane** (Огляд / Будівлі / Дії), and a separate system gates which tabs are interactable.

The tab row is a **PERMANENT** element of the context sub-panel — it sits **above** the `ContextFilled` ↔
`ContextEmpty` selection swap (owned by `HexInfoPanel`), so the tabs stay visible even with no hex selected. The
three content panes (`OverviewPane` / `BuildingsPane` / `ActionsPane`) live inside `ContextFilled`; this window
owns **which pane is shown**, `HexInfoPanel` owns whether `ContextFilled` is shown at all. See
`HexInfoPanel/HEX_INFO_PANEL.md` ("Ownership split").

Tokens, component patterns, USS construction: `GENERAL_UI_STYLE.md`. This doc adds ONLY what is specific to
this control — do not restate tokens or principles. Code STRUCTURE (signatures, deps) is in graphify.

## Current state — implemented; one Unity-side wiring step left
The components, event, enum, view, spawn subsystem and two systems exist and are wired in C#, **and the markup is
authored** in the shared `UI/MainUI` document (`Prefabs/HexInfoPanel.uxml` / `.uss`): the permanent tab row, the
three content panes, and the pane swap in `SetActive`. There is no AP / economy model — availability is a stub
(all tabs always enabled); `BuildingsPane` / `ActionsPane` are empty named containers pending content.

**Pending (Unity-side, NOT a code task):** the `ContextTabsView` MonoBehaviour must be added to the `UI/MainUI`
prefab and its `PanelRenderer` field assigned. Until then `ContextTabsSpawnSubSystem` throws "ContextTabsView is
missing from the Main UI prefab" (`GetComponentInChildren<ContextTabsView>` returns null — this is the prefab
component, not the UXML markup).

## Markup contract
The view (`ContextTabsView`) resolves three `Toggle`s **and** three content panes off the shared Main UI
`PanelRenderer` by name. The markup authors (already in `HexInfoPanel.uxml`):
- `ui:Toggle` elements named **`TabOverview`**, **`TabBuildings`**, **`TabActions`**, class `ctx-tab`, inside the
  **permanent** `ContextTabs` row (a sibling above `ContextFilled`/`ContextEmpty`, so it never hides on no
  selection);
- content panes named **`OverviewPane`**, **`BuildingsPane`**, **`ActionsPane`** (class `ctx-pane`) inside
  `ContextFilled`. `OverviewPane` is authored visible; `BuildingsPane`/`ActionsPane` are authored `display:none`
  so UI Builder previews the default-active pane;
- the active tab is shown via the **`:checked`** pseudo-state (USS), NOT a class — the view drives each toggle's
  value via `SetValueWithoutNotify`. `TabOverview` is authored `value="true"` to match the seeded default;
- the native Toggle chrome (`.unity-toggle__checkmark`, the empty `.unity-base-field__label`) is hidden in USS so
  only the tab text shows.

## Flow (toggle → events → change)
1. **Click** — a tab fires the Toggle `ChangeEvent`. Single-select rule: turning the **active** tab off is
   reverted (`SetValueWithoutNotify(true)` — selection is never empty); on the **on**-edge of any other tab the
   view records the chosen tab into the `ActiveContextTabComponent` **world singleton** (`World.Set`), optimistically
   reflects it (`SetActive`, so two tabs are never checked at once), then raises a **payload-less** one-frame
   `ContextTabChangedEvent` pulse (+ `EventTag`). The event carries no data by design (pulse + reconcile against state).
2. **Selection** — `ContextTabSelectionSystem` (reactive, anchored on the event set) reads the current
   `ActiveContextTabComponent` and reconciles the view via `view.SetActive(active)`, which highlights the checked
   tab **and shows the matching content pane** (hiding the other two). Keeping the trigger in a system (not the
   MonoBehaviour) mirrors the `EndTurnView`/`EndTurnSystem` split; fail-loud if the state is missing or `Unknown`.
   Idempotent.
3. **Availability** — `ContextTabsAvailabilitySystem` (**reactive** on the `SelectedHexChangedEvent` pulse
   raised by `UserInput.HexSelectionSystem`; reads the world view singleton + whether a `HexSelectedComponent`
   exists) pushes `view.SetTabEnabled(tab, IsAvailable(tab, hasSelection))` for the three real tabs.
   `IsAvailable` is a **stub returning true** — now keyed to selection so real rules drop in later; the
   pre-selection state is the UIElements default (all enabled), so no spawn seed is needed.

`ContextTab` is `Unknown / Overview / Buildings / Actions` — `Unknown = 0` is the sentinel so
`default(ContextTab)` is never a valid tab. The seeded default active tab is **Overview**
(`ContextTabsSpawnSubSystem`).

## Implementation (assembly `Presentation.UI`, window `ContextTabs/`)
Mirrors the EndTurn pattern; the click-emit mirrors `EndTurnView` / the generator UI.
- **`ContextTabsView`** (`Views/`) — MonoBehaviour over the **shared** Main UI `PanelRenderer`. `[Inject]
  Construct(World)`. Binds each tab's `ChangeEvent<bool>`; on a real selection writes `ActiveContextTabComponent`
  (`World.Set`) then creates an entity with a payload-less `ContextTabChangedEvent` + `EventTag`. Exposes
  `SetActive(ContextTab)` — mirrors the active tab via `SetValueWithoutNotify` (`:checked` does the highlight)
  **and** toggles the three content panes' `display` so only the active pane shows — and
  `SetTabEnabled(ContextTab, bool)` (`SetEnabled`). It re-applies the shared `raycast-transparent` picking rule
  like the other views.
- **`ContextTabsViewComponent`** (`Components/`) — **world singleton** referencing the view, written via
  `World.Set` (this window has **no singleton entity**). The systems read it off the world.
- **`ActiveContextTabComponent`** (`Components/`) — immutable **world singleton** (which tab is active), written
  via `World.Set` (mirrors `TurnCountComponent`). The view is its writer; the systems read it.
- **`ContextTabChangedEvent`** (`Events/`) — **payload-less** one-frame pulse; disposed by `EventCleanupSystem`.
- **`ContextTabsSpawnSubSystem`** (`Systems/`, a `MainUISpawnSubSystem`, Priority 20) — resolves the view off
  the shared `UI/MainUI` instance (`GetComponentInChildren`, fail-loud if missing), seeds the
  `ContextTabsViewComponent` + `ActiveContextTabComponent(Overview)` world singletons, applies the initial
  highlight. Creates no entity, instantiates nothing.
- **`ContextTabSelectionSystem`** (reactive `UpdatedSystem` on `ContextTabChangedEvent`, reads world state) /
  **`ContextTabsAvailabilitySystem`** (reactive `UpdatedSystem` on `SelectedHexChangedEvent`) — registered in
  `UIInstaller`, wired into GameplayState by `Boot` (after `endTurn`, before `eventCleanup` so the pulses are
  consumed before cleanup).

## Scaffold / not done
- **Prefab wiring (Unity-side)** — add the `ContextTabsView` MonoBehaviour to the `UI/MainUI` prefab + assign its
  `PanelRenderer` (see Current state). Until then the spawn subsystem throws.
- **Availability rules** — `IsAvailable` is a stub (always true). Wire real player-action gating (action
  points, ownership, turn phase) when that model lands, not as a UI task.
- **Tab content** — switching the active tab now swaps the visible pane (`OverviewPane` / `BuildingsPane` /
  `ActionsPane`). Only `OverviewPane` has real content; `BuildingsPane` / `ActionsPane` are **empty named
  containers** — their content (and any `ActiveContextTabComponent` consumers beyond the pane swap) come later.
