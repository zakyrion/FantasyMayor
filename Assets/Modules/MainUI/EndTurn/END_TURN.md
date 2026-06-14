# End Turn Button

The global turn-commit control, anchored **bottom-left** of the Gameplay HUD. Window of the `MainUI`
module (`EndTurn/`).

Tokens, component patterns, placement rules, and USS construction: `GENERAL_UI_STYLE.md`. This doc adds
ONLY what is specific to this control — do not restate tokens or principles here.

## Why this is a button at all (documented exception)
`GENERAL_UI_STYLE.md` §2/§7 forbid action buttons on STATE panels — player agency lives in Mayor cards.
The End Turn button is a **deliberate, documented exception**: it is neither an entity-state action nor a
Mayor card. It is the single GLOBAL game-flow control that commits the turn. Consequences:
- It MUST NOT live on the hex info panel (that would violate state-vs-agency).
- Its root layer is raycast-transparent; only the button itself is raycast-opaque, so map clicks pass
  through everywhere else (same picking rule as the generator UI / hex info panel).

## Placement
- Bottom-left, ~24px margin (Old World lens) — the muscle-memory anchor, always visible in Gameplay.
- It is the first piece of the bottom-left **turn cluster**. «Хід N» and the «Дії» (Action Points)
  readout are part of that cluster in the design but are **not implemented** — they have no data source
  yet (see Scaffold). The hex info panel lives bottom-RIGHT and is unrelated.
- Design reference: `design-mockups/main-screen.html`.

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
- **`EndTurnView`** (`Views/`) — MonoBehaviour over the UIDocument. `[Inject] Construct(World)`. On click
  (Ready only) creates an entity with `NextTurnEvent` + `EventTag` — the emitter `TURN.md` was missing.
  `SetProcessing(bool)` relabels, toggles `is-processing`, and `SetEnabled`. Root marked
  `raycast-transparent`; the button stays pickable.
- **`EndTurnViewComponent`** (`Components/`) — view singleton (mirrors `HexInfoPanelViewComponent`).
- **`EndTurnSpawnSubSystem`** (`Systems/`, a `MainUISpawnSubSystem` run by `MainUISpawnSystem` at pipeline
  800, Priority 10) — resolves `EndTurnView` off the shared `UI/MainUI` instance (`GetComponentInChildren`),
  publishes the component, leaves the button **hidden**. Instantiates nothing — the orchestrator owns the
  single Main UI handle.
- **`EndTurnSystem`** (`Systems/`, per-frame, wired into GameplayState by `Boot`) — each Gameplay tick
  reveals the button (idempotent `Show()`) and calls `view.SetProcessing(world.Has<TurnProcessorComponent>())`.

**Why click-emit in the View but state-reconcile in the System:** the View raising a one-frame event on
click follows the generator-UI precedent; per-frame ECS polling (the Processing mirror) must not live in a
MonoBehaviour, so the System does it — same split as `HexInfoPanelView` (dumb) + `HexInfoPanelSystem`
(reconciler). The engine's re-entry guard (`NextTurnEvent` ignored while `TurnProcessorComponent` exists)
is the authoritative backstop; the View's `_processing` self-guard is defence in depth.

## Scaffold / not done
- «Хід N» and «Дії» (Action Points) are **not built** — no turn-counter and no AP model exist yet.
- `Locked` state is future (needs phase/turn ownership).

## Current State
- **Implemented:** `EndTurnView` + `EndTurnViewComponent` + `EndTurnSpawnSubSystem` + `EndTurnSystem` +
  `EndTurnView.uxml`/`.uss`, registered in `UIInstaller`, wired into Gameplay by `Boot`.
- **User-side (Unity):** `EndTurnView` (UIDocument + the uxml) is a **child of the single `UI/MainUI`
  prefab** — there is no separate `UI/EndTurnView` address. The prefab is authored in Unity; not a code
  artifact.
