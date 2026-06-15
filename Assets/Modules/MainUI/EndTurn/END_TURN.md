# Turn Cluster (End Turn)

The bottom-left **turn cluster** card of the Gameplay HUD (Old World lens): the turn number «Хід N», a «Дії»
(action-points) readout, and the global **End Turn** button that commits the turn. Window of the `MainUI`
module (`EndTurn/`). The window keeps the `EndTurn` name even though it now hosts the whole cluster.

Tokens, component patterns, placement rules, and USS construction: `GENERAL_UI_STYLE.md`. This doc adds
ONLY what is specific to this control — do not restate tokens or principles here.

## Why this is a button at all (documented exception)
`GENERAL_UI_STYLE.md` §2/§7 forbid action buttons on STATE panels — player agency lives in Mayor cards.
The End Turn button is a **deliberate, documented exception**: it is neither an entity-state action nor a
Mayor card. It is the single GLOBAL game-flow control that commits the turn. Consequences:
- It MUST NOT live on the hex info panel (that would violate state-vs-agency).
- The shared full-screen root is raycast-transparent; the cluster card (and its button) stay raycast-opaque,
  so map clicks pass through everywhere else (same picking rule as the generator UI / hex info panel).

## Placement
- A card anchored bottom-left, ~24px margin (Old World lens) — the muscle-memory anchor, always visible in
  Gameplay. The card chrome mirrors the hex info-panel card (panel-bg + faux-gradient gold rim).
- The card stacks: **«Хід N»** (top) → **«Дії»** placeholder → **End Turn button** (full-width). «Хід N» is
  live (bound to `TurnCountComponent`); «Дії» is a visible placeholder (dashes) until the AP model lands —
  see Scaffold. The hex info panel lives bottom-RIGHT and is unrelated.
- Design reference: `design-mockups/main-screen.html` (the `.turn` cluster).

## Visual
- **Hero CTA — the one place a filled-gold button is warranted.** Body gold `rgb(217,164,65)`, dark warm
  text `rgb(37,28,14)`, bold, letter-spacing ~1.4px, radius 13. A lighter `border-top-color` fakes a
  bevel. Glow/elevation in-engine would come from a 9-slice sprite — USS has no `box-shadow`/gradient (§9).
- Label «ЗАВЕРШИТИ ХІД», uppercased in DATA (USS has no `text-transform`, §9).

## States
Switched by class/`display` (§10). Two states now, one future.

| State | When | Look | Interactive |
|---|---|---|---|
| **Ready** | no turn running | filled gold, «ЗАВЕРШИТИ ХІД» | yes — click raises `NextTurnEvent` |
| **Processing** | a turn is running | dimmed gold + `is-processing`, «ОБРАХУНОК ХОДУ…», `SetEnabled(false)` | no — mirrors the re-entry guard |
| Locked *(future)* | phase/turn ownership exists | like Processing | no |

## Implementation (module `MainUI`, window `EndTurn/`)
Mirrors the HexInfoPanel panel pattern; the click-emit mirrors the generator UI (`HexesUI`).
- **`EndTurnView`** (`Views/`) — MonoBehaviour over the **shared** Main UI `UIDocument` (the same document as
  the hex info panel — see `MAIN_UI.md`). `[Inject] Construct(World)`. On click (Ready only) creates an entity
  with `NextTurnEvent` + `EventTag` — the emitter `TURN.md` was missing. `Show/Hide` toggle the **cluster
  card's** `display` (the `TurnCluster` element, NOT the document root — that would blank the whole Main UI).
  `SetTurnNumber(int)` writes «Хід N»; `SetProcessing(bool)` relabels the button, toggles `is-processing`, and
  `SetEnabled`. The shared `Root` is marked `raycast-transparent`; the button stays pickable, and the card
  itself stays pickable so it blocks map clicks like the info-panel card.
- **`EndTurnViewComponent`** (`Components/`) — view singleton (mirrors `HexInfoPanelViewComponent`).
- **`EndTurnSpawnSubSystem`** (`Systems/`, a `MainUISpawnSubSystem` run by `MainUISpawnSystem` at pipeline
  800, Priority 10) — resolves `EndTurnView` off the shared `UI/MainUI` instance (`GetComponentInChildren`),
  publishes the component, leaves the button **hidden**. Instantiates nothing — the orchestrator owns the
  single Main UI handle.
- **`EndTurnSystem`** (`Systems/`, per-frame, wired into GameplayState by `Boot`) — each Gameplay tick
  reveals the cluster (idempotent `Show()`), mirrors `view.SetProcessing(world.Has<TurnProcessorComponent>())`,
  and pushes `view.SetTurnNumber(world.Get<TurnCountComponent>().Value)` (throws if the counter is unseeded —
  fail-loud). The turn counter itself lives in module `Turn` (`TurnCountComponent` / `TurnCompletedEvent` /
  `TurnCountSystem`) — see `TURN.md`; this window only displays it.

**Why click-emit in the View but state-reconcile in the System:** the View raising a one-frame event on
click follows the generator-UI precedent; per-frame ECS polling (the Processing mirror) must not live in a
MonoBehaviour, so the System does it — same split as `HexInfoPanelView` (dumb) + `HexInfoPanelSystem`
(reconciler). The engine's re-entry guard (`NextTurnEvent` ignored while `TurnProcessorComponent` exists)
is the authoritative backstop; the View's `_processing` self-guard is defence in depth.

## Scaffold / not done
- «Хід N» is **live** (module `Turn`: `TurnCountComponent`).
- «Дії» (Action Points) is a **visible placeholder only** — the block is authored and shown with dash values,
  but **no AP model exists yet** and no system touches it. Wire it when the action-points mechanic lands
  (`GAMEPLAY_FOUNDATION.md` level), not as a UI task.
- `Locked` state is future (needs phase/turn ownership).

## Current State
- **Implemented:** `EndTurnView` + `EndTurnViewComponent` + `EndTurnSpawnSubSystem` + `EndTurnSystem`,
  registered in `UIInstaller`, wired into Gameplay by `Boot`. The cluster **markup lives in the shared Main UI
  document** — `TurnCluster` (card with `TurnNumber`, the «Дії» placeholder, and `EndTurnButton`) is a sibling
  of the info-panel card in `Prefabs/HexInfoPanel.uxml`, styled by `Prefabs/HexInfoPanel.uss`. There is no
  standalone `EndTurnView.uxml`/`.uss` (merged away). «Хід N» binds to `TurnCountComponent` (module `Turn`).
- **User-side (Unity):** `EndTurnView` is a MonoBehaviour on the single `UI/MainUI` prefab, referencing the
  **same `UIDocument`** as `HexInfoPanelView`. There is no separate `UI/EndTurnView` address. The prefab is
  authored in Unity; not a code artifact. (The orphan `EndTurnView.uxml.meta`/`.uss.meta` left by the merge
  are removed in Unity.)
