---
category: A
read: reference
tags: [actions, turn, ecs]
related:
  - "[ACTIONS](ACTIONS.md)"
  - "[TURN](../../Modules/Turn/TURN.md)"
status: partial
---

# Turn Phases

The Actions domain's turn-phase subsystems: phase **content** that plugs into the `Turn` engine.
Currently one phase — the Mayor Action-Points restore.

## Purpose
Per the domain's "turn engine reuse" decision (`ACTIONS.md`), Actions does not run turns; it supplies
`TurnPhaseSubSystem`s that the `Turn` module's `TurnProcessorSystem` executes on each turn pulse. Each
phase is registered in `ActionsInstaller` as a `TurnPhaseSubSystem`, and VContainer collects them into
the list the processor runs.

## Public Contract & Gotchas
- **AP restore is a SET, not an accumulate.** `MayorActionPointsRestoreSubSystem` resets every Mayor's
  live `ActionPoint` resource stack to the Mayor's per-turn restore value at the start of each new turn.
  Action Points do **not** carry over between turns, so the phase overwrites the stack to full — it does
  not add to it.
- **Off-thread compute, main-thread write.** The phase runs on the turn thread pool but switches to the
  main thread before any world write — the `Turn` engine invariant (`TURN.md`). It reads the Mayor row
  and re-Sets the matching `ActionPoint` stack via the Mayor-id index.
- **Phase ordering is the Upkeep band.** It runs in the Upkeep portion of the turn so AP is full before
  any player-action phase would spend it (spending phases are not built yet).

## Current State
PARTIAL. `MayorActionPointsRestoreSubSystem` is implemented and wired. The other turn phases
(cross-domain upkeep arithmetic, resolution, yield split) are scaffold. AP **spending** mechanics do not
exist yet — only restore.
