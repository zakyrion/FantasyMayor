---
category: B
read: reference
tags: [ui, turn]
related:
  - "[MAIN_UI](../MAIN_UI.md)"
  - "[HEX_INFO_PANEL](../HexInfoPanel/HEX_INFO_PANEL.md)"
  - "[TURN](../../Turn/TURN.md)"
  - "[GENERAL_UI_STYLE](../../../../GENERAL_UI_STYLE.md)"
---

# Turn Corner (End Turn) — Turn Sub-Panel

The **TURN sub-panel** (left) of the shared bottom panel (`GENERAL_UI_STYLE.md` §4): the turn number «Хід N», two
AP tiles («Дії зараз» / «наст. хід»), and the global **End Turn** button pinned to the bottom. Window of the `MainUI`
module (`EndTurn/`). The window keeps the `EndTurn` name even though it now owns the whole turn corner **and**
the bottom-panel shell reveal.

Tokens, component patterns, placement rules, and USS construction: `GENERAL_UI_STYLE.md`. This doc adds ONLY
what is specific to this control — do not restate tokens or principles here.

## Why this is a button (turn-commit action)
The End Turn button is the single GLOBAL game-flow control that commits the turn (`GENERAL_UI_STYLE.md` §9 —
panels carry actions; this is the most prominent one). It is not tied to any selected entity. Consequences:
- It lives on the TURN sub-panel (global), not the context (hex) sub-panel — the context panel hosts the
  selected hex's own actions, not global game-flow.
- The shared full-screen root is raycast-transparent; the bottom panel (and the button) stay raycast-opaque, so
  map clicks pass through everywhere else (same picking rule as the generator UI / hex info panel).

## Placement
- The **left sub-panel** of the always-present bottom panel, divided from the context sub-panel by a vertical
  divider. It never appears/disappears — the muscle-memory turn corner.
- The sub-panel stacks top-to-bottom: **«Хід N»** → **two AP tiles** («Дії зараз» / «наст. хід») →
  **End Turn button**. The button is pinned to the bottom with `margin-top: auto`, so the two sub-panels share a
  common bottom edge. «Хід N» is live (bound to `TurnCountComponent`); the two AP tiles are visible placeholders
  (dashes) until the AP model lands — see Scaffold. No «ПОТОЧНИЙ ХІД» cap, no season / phase
  (`GENERAL_UI_STYLE.md` §4).
- Design reference: `design-mockups/FantasyMayor-HUD.html` (the `.turn` sub-panel).

## Visual
- **Hero CTA — the one place a filled-gold button is warranted.** Body gold `rgb(217,164,65)`, dark warm text
  `rgb(37,28,14)`, bold, letter-spacing ~2px, radius 15 (+50% scale). A lighter `border-top-color` fakes a
  bevel. Glow/elevation in-engine would come from a 9-slice sprite — USS has no `box-shadow`/gradient (§11).
- Label «ЗАВЕРШИТИ ХІД», uppercased in DATA (USS has no `text-transform`, §11).

## States
Switched by class/`display` (§12). Two states now, one future.

| State | When | Look | Interactive |
|---|---|---|---|
| **Ready** | no turn running | filled gold, «ЗАВЕРШИТИ ХІД» | yes — click raises `NextTurnEvent` |
| **Processing** | a turn is running | dimmed gold + `is-processing`, «ОБРАХУНОК ХОДУ…», `SetEnabled(false)` | no — mirrors the re-entry guard |
| Locked *(future)* | phase/turn ownership exists | like Processing | no |

## Implementation (module `MainUI`, window `EndTurn/`)
Mirrors the HexInfoPanel pattern; the click-emit mirrors the generator UI (`HexesUI`).
- **`EndTurnView`** (`Views/`) — MonoBehaviour over the **shared** Main UI `PanelRenderer` (the same document as
  the hex info panel — see `MAIN_UI.md`). `[Inject] Construct(World)`. On click (Ready only) creates an entity
  with `NextTurnEvent` + `EventTag` — the emitter `TURN.md` was missing. **Owns the shell reveal:** `Show/Hide`
  toggle the whole **`BottomPanel`** shell's `display` (the turn corner is its always-present part), NOT the
  document root — that would blank the whole Main UI. The context sub-panel's content is swapped independently
  by `HexInfoPanelView`. `SetTurnNumber(int)` writes «Хід N»; `SetProcessing(bool)` relabels the button, toggles
  `is-processing`, and `SetEnabled`. The shared `Root` is marked `raycast-transparent`; the button stays
  pickable, and the bottom panel itself stays pickable so it blocks map clicks like the info-panel did.
- **`EndTurnViewComponent`** (`Components/`) — view singleton (mirrors `HexInfoPanelViewComponent`).
- **`EndTurnSpawnSubSystem`** (`Systems/`, a `MainUISpawnSubSystem` run by `MainUISpawnSystem` at pipeline 800,
  Priority 10) — resolves `EndTurnView` off the shared `UI/MainUI` instance (`GetComponentInChildren`),
  publishes the component, leaves the whole **bottom panel hidden**. Instantiates nothing.
- **`EndTurnSystem`** (`Systems/`, per-frame, wired into GameplayState by `Boot`) — each Gameplay tick reveals
  the bottom panel (idempotent `Show()` — it owns the shell reveal), mirrors
  `view.SetProcessing(world.Has<TurnProcessorComponent>())`, and pushes
  `view.SetTurnNumber(world.Get<TurnCountComponent>().Value)` (throws if the counter is unseeded — fail-loud).
  The turn counter itself lives in module `Turn` (`TurnCountComponent` / `TurnCompletedEvent` /
  `TurnCountSystem`) — see `TURN.md`; this window only displays it.

**Why click-emit in the View but state-reconcile in the System:** the View raising a one-frame event on click
follows the generator-UI precedent; per-frame ECS polling (the Processing mirror) must not live in a
MonoBehaviour, so the System does it — same split as `HexInfoPanelView` (dumb) + `HexInfoPanelSystem`
(reconciler). The engine's re-entry guard (`NextTurnEvent` ignored while `TurnProcessorComponent` exists) is the
authoritative backstop; the View's `_processing` self-guard is defence in depth.

## Scaffold / not done
- «Хід N» is **live** (module `Turn`: `TurnCountComponent`).
- The **two AP tiles** («Дії зараз» / «наст. хід») are **visible placeholders only** — authored and shown with
  dash values, but **no AP model exists yet** and no system touches them. Wire them when the action-points
  mechanic lands (`GAMEPLAY_FOUNDATION.md` level), not as a UI task.
- `Locked` state is future (needs phase/turn ownership).

## Current State
- **Implemented:** `EndTurnView` + `EndTurnViewComponent` + `EndTurnSpawnSubSystem` + `EndTurnSystem`, registered
  in `UIInstaller`, wired into Gameplay by `Boot`. The turn corner **markup lives in the shared Main UI
  document** — `TurnPanel` (the `.bp-turn` sub-panel with `TurnNumber`, the two AP tiles, and
  `EndTurnButton`) is the left sub-panel of the `BottomPanel` shell in `Prefabs/HexInfoPanel.uxml`, styled by
  `Prefabs/HexInfoPanel.uss`. There is no standalone `EndTurnView.uxml`/`.uss`. «Хід N» binds to
  `TurnCountComponent` (module `Turn`); the two AP tiles are static placeholders (no system touches them).
- **User-side (Unity):** `EndTurnView` is a MonoBehaviour on the single `UI/MainUI` prefab, referencing the
  **same `PanelRenderer`** as `HexInfoPanelView`. There is no separate `UI/EndTurnView` address. The prefab is
  authored in Unity; not a code artifact.
