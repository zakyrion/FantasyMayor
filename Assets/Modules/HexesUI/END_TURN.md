# End Turn Button

The global turn-commit control plus a compact turn indicator, anchored bottom-right of the Gameplay HUD.

Tokens, component patterns, placement rules, and USS construction: `GENERAL_UI_STYLE.md`. This doc adds
ONLY what is specific to this control — do not restate tokens or principles here.

## Why this is a button at all (documented exception)
`GENERAL_UI_STYLE.md` §2/§7 forbid action buttons on STATE panels — player agency lives in Mayor cards.
The End Turn button is a **deliberate, documented exception**: it is neither an entity-state action nor a
Mayor card. It is the single GLOBAL game-flow control that commits the turn. Consequences:
- It MUST NOT live on the hex info panel (that would violate state-vs-agency).
- It is the ONLY persistent interactive (raycast-opaque) element of the right HUD column; the hex info
  panel stays raycast-transparent (`HEXES_UI.md` invariant).

## Placement & composition
- Right column, bottom-anchored (§6). The button owns the fixed bottom-right corner (~24px margin) — the
  muscle-memory anchor, always visible.
- The hex info panel stacks ABOVE the button (same right edge, ~12px gap) and appears only on hex
  selection. The button never moves; only the panel comes and goes above it.
- A compact turn indicator («Хід N») sits between the panel and the button.
- Bottom-center is RESERVED for the future Mayor card hand — corner placement keeps the layout
  forward-compatible. The map center stays clear.

## Visual
- **Hero CTA — the one place a filled-gold button is warranted.** Body `--gold-soft`, dark warm text
  (~`#251c0e`), sans, UPPERCASE, letter-spacing ~1.4px, radius 13 (≈ the panel's 14). A lighter
  `border-top` gives a faux-bevel that survives USS. Glow/elevation in-engine come from a 9-slice sprite —
  USS has no `box-shadow`, gradient, or gradient-border (§9).
- Icon: a forward/commit glyph (sprite). Label string «ЗАВЕРШИТИ ХІД», uppercased in DATA (USS has no
  `text-transform`, §9).
- Turn indicator: «Хід N», serif, `--txt`, small, above the button.

## States (content model)
Pre-author all states; switch by toggling a class / `display` (§10). Two states now, one future.

| State | When | Look | Interactive |
|---|---|---|---|
| **Ready** | player's Mayor Phase, no turn running | filled gold, «ЗАВЕРШИТИ ХІД» | yes — click raises `NextTurnEvent` |
| **Processing** | a turn is running | dimmed gold `rgba(217,164,65,0.14)` + gold border, «ОБРАХУНОК ХОДУ…» + spinner | no — this IS the re-entry guard |
| Locked *(future)* | phase/turn ownership exists | like Processing, no spinner | no |

## Behaviour & ECS binding
- **Click (Ready)** → create an entity with `NextTurnEvent` + `EventTag`. This is the deferred emitter
  named in `TURN.md`: it ends the Mayor Phase and hands control to the Turn pipeline.
- **Processing** is driven by the presence of `TurnProcessorComponent` (world component, module `Turn`).
  Present → Processing + non-interactive (mirrors the engine's re-entry guard). Removed (turn done) →
  back to Ready for the next turn.
- The turn indicator «Хід N» needs a turn-count source that **does not exist yet** — SCAFFOLD. Until a
  turn-number component/world component lands, show «Хід 1» static or hide the counter.

## Implementation binding (planned — OPEN)
- **Owning module / render layer is OPEN.** Candidates: extend `HexesUI` (current UI-window home) or a
  small dedicated HUD module. Decide at implementation. The emitter system raises Turn's `NextTurnEvent`;
  the state-watch system reads `TurnProcessorComponent`.
- The «Хід N» turn-count source is undefined — decide when the turn loop's counter lands.

## Current State
- **DESIGN ONLY.** No UXML/USS/prefab/system exists. Reference render: the composition mockup produced in
  the design session (hex info panel above a gold End Turn button, Ready + Processing states).
- Ready / Processing states are specified; Locked is future.
