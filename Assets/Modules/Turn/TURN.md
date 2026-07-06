---
category: A
read: reference
tags:
  - turn
  - ecs
  - gameplay
related:
  - "[MAIN_UI](../../Presentation/UI/MAIN_UI.md)"
  - "[END_TURN](../../Presentation/UI/EndTurn/END_TURN.md)"
status: partial
code_refs:
  systems:    [TurnProcessorSystem, TurnCountSystem, EventCleanupSystem, MayorActionPointsRestoreSubSystem]
  components: [TurnProcessorComponent, TurnCountComponent]
  events:     [NextTurnEvent, TurnCompletedEvent]
  interfaces: [TurnPhaseSubSystem]
  types:      [TurnPhaseRunner]
  views:      [EndTurnView]
---

# Turn

Engine that runs a game turn: on a pulse it fires ordered phase subsystems off-thread, gating other systems.

## Purpose
Infrastructure the gameplay domains consume. The turn phases of `GAMEPLAY_FOUNDATION.md`
(Start Preview → Mayor → Citizen → Resolution → Upkeep → Consequences) plug in here later as
phase subsystems; this module is only the runner + lifecycle, no phase logic.

## Phase Order (planned — only the Upkeep AP-restore phase is live so far)
The engine runs phases on a `NextTurnEvent` pulse, i.e. AFTER the player has finished acting. So the
runtime order is NOT the presentation order in `GAMEPLAY_FOUNDATION.md`. By ascending `Priority`:

`Citizen (3) → Resolution (4) → Upkeep (5) → Consequences (6) → Preview (1)`

- **Mayor Phase (2) is NOT a phase subsystem.** It is the interactive player↔game layer (panels, submenus,
  input, UI) running as ordinary Gameplay systems. Its terminal "end turn" action is the emitter that raises
  `NextTurnEvent` and hands control to this pipeline.
- **The player interacts ONLY in the Mayor Phase.** The whole pipeline — Citizen Phase included — is
  non-interactive AI/economy computation. This is exactly WHY it can run off the main thread. A
  mechanic needing player input mid-resolve would break the off-thread model and must be designed
  separately.
- **Preview (1) sits at the TAIL:** it computes the snapshot the player reads at the start of the next
  Mayor Phase (available actions, needs, signals), reflecting the Consequences just produced.
- **The first turn intentionally starts EMPTY.** There is NO bootstrap Preview. The first Mayor Phase
  opens on an empty snapshot; Preview only ever runs as the pipeline tail. Do NOT add a startup
  preview build.

## Trigger
`TurnProcessorSystem` consumes the one-frame `NextTurnEvent`. It is NOT a `WhenAdded`/reactive
set — it queries `With<NextTurnEvent>` from a per-frame poller (see Design Decisions). The emitter is
the **End Turn button** (`EndTurnView`, assembly `Presentation.UI`): clicking it in the Mayor Phase creates
a `NextTurnEvent` + `EventTag` entity. Full producer→consumer flow: the ecs-graph (`/ecs-graph`).

## Public Contract & Gotchas
- **Thread boundary (hard invariant).** The phase run executes on the thread pool
  (`UniTask.RunOnThreadPool`) and must never touch the Unity API. Every world write — `Set`/`Remove`
  of `TurnProcessorComponent`, and any future phase world-write — happens back on the main thread
  (`await UniTask.SwitchToMainThread()` first). The pool *computes*; the main thread *writes*.
- **`TurnProcessorComponent` lifecycle.** Set to `Running` on a pulse → flipped to `Completed` by the
  background run (on the main thread) → removed by `TurnProcessorSystem` on a later tick. Its
  PRESENCE is the "turn in progress" gate; absence means idle.
- **Re-entry guard.** While `TurnProcessorComponent` exists, `NextTurnEvent` pulses are ignored — only
  one turn runs at a time.
- **Turn boundary signal.** When `TurnProcessorSystem` removes `TurnProcessorComponent` (turn resolved) it
  raises a one-frame `TurnCompletedEvent` (+ `EventTag`). This is the reusable "a turn just finished" pulse —
  turn-boundary reactors subscribe to it instead of re-deriving completion from the processor's status.
- **Turn counter.** `TurnCountComponent` (world singleton, `int Value`) is the current turn number. Seeded to
  `1` on Gameplay enter (`GameplayState.EnterAsync`) — the first Mayor Phase is turn 1 — and incremented by
  `TurnCountSystem` on each `TurnCompletedEvent`. `TurnCountSystem` runs at Priority 1010 (above the processor's
  1000) so it reads the pulse the same frame it is emitted, before `EventCleanupSystem` clears it. The MainUI
  turn cluster reads this to show "Хід N".
- **Cancellation.** The run observes `Core.StatusMonitor.Token` (global app-shutdown token), so an
  in-flight turn stops cleanly on quit. It is the only quiet stop.
- **`TurnPhaseRunner` is stateless and threading-agnostic.** It only orders by `Priority`, skips
  `!IsEnabled`, and awaits phases sequentially. The launcher owns the pool hop, so a nested
  phase-orchestrator reuses the runner on the same pool thread with no extra hop.

## Non-Obvious Invariants
- **Nesting is by reuse, not a recursive base.** A phase that needs children is itself an orchestrator
  over its own `TurnPhaseSubSystem` list, reusing `TurnPhaseRunner`. There is no separate composite
  type. Concrete phases carry intent names (`MayorActionPointsRestoreSubSystem`, …) so the tree stays
  explicit and readable.

## Design Decisions
- **`TurnProcessorSystem` is a deliberate per-frame system** (implements `IUpdatedSystem` directly, no
  entity set), NOT a reactive entity-set system. It must tick every frame to poll the in-flight task's
  completion; a set anchored on `With<NextTurnEvent>` is empty on most frames and would never poll.
  This is the written justification `ARCHITECTURE.md` requires for any per-frame system.
- **State lives in a component, not the system.** The in-flight marker, status, and "turn in progress"
  signal are all `TurnProcessorComponent` (a world component). The system stays stateless.

## Current State
- **FIRST PHASE LIVE.** `TurnPhaseSubSystem` has one concrete descendant:
  `MayorActionPointsRestoreSubSystem` (domain `Actions`), which resets the Mayor's `ActionPoint` resource
  stack each turn. The empty-list `RegisterInstance` was removed from `TurnInstaller`; phases are now
  collected by VContainer from the `.As<…, TurnPhaseSubSystem>()` registrations (the phase's owning domain
  registers it — here `ActionsInstaller`).
- Two `Debug.Log` lines (turn started / completed) still exist to make the pipeline observable in Play
  mode — remove once the phase set is mature.
- The `NextTurnEvent` emitter now exists: the MainUI End Turn button (`EndTurnView`). The button also
  reflects pipeline state — it shows "Processing" while `TurnProcessorComponent` is present.
- The **turn counter is live**: `TurnCountComponent` + `TurnCompletedEvent` + `TurnCountSystem`. Each
  completed turn raises `TurnCompletedEvent` and bumps "Хід N" in the MainUI turn cluster.
- **Adding a phase:** register it `.As<PhaseN, TurnPhaseSubSystem>()` in its owning domain's installer
  (pattern: `ActionsInstaller`), exactly as `HexResourcesViewInstaller` does for view subsystems. Do NOT
  re-introduce the empty-list `RegisterInstance` in `TurnInstaller`.
