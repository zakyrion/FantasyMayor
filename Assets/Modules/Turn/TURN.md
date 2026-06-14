# Turn

Engine that runs a game turn: on a turn pulse it fires an ordered set of phase subsystems off the main thread and signals "a turn is being processed" so other systems can gate.

## Purpose
Infrastructure the gameplay domains consume. The turn phases of `GAMEPLAY_FOUNDATION.md`
(Start Preview → Mayor → Citizen → Resolution → Upkeep → Consequences) plug in here later as
phase subsystems; this module is only the runner + lifecycle, no phase logic.

## Phase Order (planned — no phases exist yet)
The engine runs phases on a `NextTurnEvent` pulse, i.e. AFTER the player has finished acting. So the
runtime order is NOT the presentation order in `GAMEPLAY_FOUNDATION.md`. By ascending `Priority`:

`Citizen (3) → Resolution (4) → Upkeep (5) → Consequences (6) → Preview (1)`

- **Mayor Phase (2) is NOT a phase subsystem.** It is the interactive player↔game layer (cards, input,
  UI) running as ordinary Gameplay systems. Its terminal "end turn" action is the emitter that raises
  `NextTurnEvent` and hands control to this pipeline.
- **The player interacts ONLY in the Mayor Phase.** The whole pipeline — Citizen Phase included — is
  non-interactive AI/economy computation. This is exactly WHY it can run off the main thread. A
  mechanic needing player input mid-resolve would break the off-thread model and must be designed
  separately.
- **Preview (1) sits at the TAIL:** it computes the snapshot the player reads at the start of the next
  Mayor Phase (available cards, needs, signals), reflecting the Consequences just produced.
- **The first turn intentionally starts EMPTY.** There is NO bootstrap Preview. The first Mayor Phase
  opens on an empty snapshot; Preview only ever runs as the pipeline tail. Do NOT add a startup
  preview build.

## Trigger
`TurnProcessorSystem` consumes the one-frame `NextTurnEvent`. It is NOT a `WhenAdded`/reactive
set — it queries `With<NextTurnEvent>` from a per-frame poller (see Design Decisions). The emitter is
the **MainUI End Turn button** (`EndTurnView`, module `MainUI`): clicking it in the Mayor Phase creates
a `NextTurnEvent` + `EventTag` entity. Full producer→consumer flow: `ECS_REFERENCE.md`.

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
- **Cancellation.** The run observes `Core.StatusMonitor.Token` (global app-shutdown token), so an
  in-flight turn stops cleanly on quit. It is the only quiet stop.
- **`TurnPhaseRunner` is stateless and threading-agnostic.** It only orders by `Priority`, skips
  `!IsEnabled`, and awaits phases sequentially. The launcher owns the pool hop, so a nested
  phase-orchestrator reuses the runner on the same pool thread with no extra hop.

## Non-Obvious Invariants
- **Nesting is by reuse, not a recursive base.** A phase that needs children is itself an orchestrator
  over its own `TurnPhaseSubSystem` list, reusing `TurnPhaseRunner`. There is no separate composite
  type. Concrete phases are named (`Phase1SubSystem`, …) so the tree stays explicit and readable.

## Design Decisions
- **`TurnProcessorSystem` is a deliberate per-frame system** (implements `IUpdatedSystem` directly, no
  entity set), NOT a reactive entity-set system. It must tick every frame to poll the in-flight task's
  completion; a set anchored on `With<NextTurnEvent>` is empty on most frames and would never poll.
  This is the written justification `ARCHITECTURE.md` requires for any per-frame system.
- **State lives in a component, not the system.** The in-flight marker, status, and "turn in progress"
  signal are all `TurnProcessorComponent` (a world component). The system stays stateless.

## Current State
- **SKELETON.** The engine runs but there are ZERO phases: `TurnPhaseSubSystem` has no concrete
  descendants, and `TurnInstaller` injects an explicit empty phase list. A turn therefore starts, runs
  an empty pool body, and completes immediately.
- Two `Debug.Log` lines (turn started / completed) exist ONLY to make the empty skeleton observable in
  Play mode — remove when real phases land.
- The `NextTurnEvent` emitter now exists: the MainUI End Turn button (`EndTurnView`). The button also
  reflects pipeline state — it shows "Processing" while `TurnProcessorComponent` is present.
- **Migration:** when the first `PhaseNSubSystem` lands, remove the empty-list `RegisterInstance` in
  `TurnInstaller` and register each phase `.As<PhaseN, TurnPhaseSubSystem>()`, exactly as
  `HexResourcesViewInstaller` does for view subsystems.
