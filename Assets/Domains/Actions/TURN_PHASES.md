---
category: A
read: reference
tags: [actions, turn, ecs]
related:
  - "[ACTIONS](ACTIONS.md)"
  - "[TURN](../../Modules/Turn/TURN.md)"
status: partial
code_refs:
  systems: [MayorAPRestoreSubSystem, TurnProcessorSystem]
  components: [MayorAPComponent, MayorAPRestoreComponent]
---

# Turn Phases

The Actions domain's turn-phase subsystems: phase **content** that plugs into the `Turn` engine.
Currently one phase — the Mayor Action-Points restore.

## Purpose
Per the domain's "turn engine reuse" decision (`ACTIONS.md`), Actions does not run turns; it supplies
`TurnPhaseSubSystem`s that the `Turn` engine executes on each turn pulse. (Which phases are registered
and how they are collected: `dig.py installer ActionsInstaller`.)

## Public Contract & Gotchas
- **AP restore is a SET, not an accumulate.** `MayorAPRestoreSubSystem` resets every Mayor's
  `MayorAPComponent.Value` to `MayorAPRestoreComponent.Value` at the start of each new turn.
  Action Points do **not** carry over between turns, so the phase overwrites the value to full — it does
  not add to it.
- **Both AP components live on the same Mayor row.** `MayorAPRestoreComponent` (the restore rule) and
  `MayorAPComponent` (the live value) sit on the one Mayor entity — the phase reads and re-Sets them
  directly, no cross-entity FK join or Table Rule lookup involved.
- **Off-thread compute, main-thread write — FLAGGED.** The doc comment states the phase switches to the
  main thread before any world write (the `Turn` engine invariant, `TURN.md`), but the actual
  `UniTask.SwitchToMainThread` call in `MayorAPRestoreSubSystem.Update` is commented out in code. Doc
  intent vs current code disagree here — human review needed, not silently reconciled.
- **Phase ordering is the Upkeep band.** It runs in the Upkeep portion of the turn so AP is full before
  any player-action phase would spend it (spending phases are not built yet).

## Current State
PARTIAL. `MayorAPRestoreSubSystem` is implemented and wired. The other turn phases
(cross-domain upkeep arithmetic, resolution, yield split) are scaffold. AP **spending** mechanics do not
exist yet — only restore.
