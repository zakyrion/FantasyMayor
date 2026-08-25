---
name: fantasymayor-session-start
description: Load and resume working context for the FantasyMayor repository by reading INDEX.md (the generated doc map), reading the docs it marks read always, running the ECS/DI graph and doc-lint health checks, reconstructing where work stopped, and ending by asking the user what to do next. Use when starting a new session in this repository, when the user asks to resume context, continue prior work, or find the current roadmap before making changes.
---

# FantasyMayor Session Start (Codex)

## Overview

Orient via the generated doc map, reconstruct the current project state from it, then hand
control back to the user with a concise status summary and a direct question about the next task.
This is the **Research / orientation entry** of the Research → Plan → Execute contract
(`AGENTS.md`, autoloaded) — it does not plan or execute.

`INDEX.md` drives all navigation: do not preload anything it does not send you to, do not invent
a different onboarding flow, and do not start implementing before the user picks the next task —
unless they asked for implementation in the same request.

Your standing process contract (HARD GATE, go/done contracts, notation, policies) is already
autoloaded from the repo `AGENTS.md` — this skill does not restate it; it only runs the startup
procedure.

## Workflow

### 1. Load INDEX.md first — the doc map

Read `INDEX.md` before anything else (plain file read). It is the single key to every doc +
canvas: each one's read-priority (`always` / `trigger` / `reference`) plus a one-line description.

Then follow INDEX's read-priority:
- Read every `read: always` doc next. The set is dynamic: it contains the standing always-docs
  plus every active Category A FLOW (`status: partial`). `CLAUDE.md` is the Claude-side counterpart
  of your autoloaded `AGENTS.md` — do NOT re-read that one file, because its contract is already in
  context. Read every other member, including every active FLOW.
  (`DOC_STANDARD.md` is `read: trigger`, not always — load it only when you author or review a doc
  yourself.)
- Open `trigger` docs only when their condition holds, and `reference` docs on demand —
  never preload them.

Respect the repository constraints you find (startup order, Unity build restrictions, module-folder
rules, read-on-demand references such as Addressables patterns).

### 1a. Graph health check

Run `python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py stats` and
`python3 ~/.claude/skills/di-graph/scripts/dig.py stats` from the project root (2 cheap CLI calls —
the scripts are shared, agent-agnostic tools, as your installed `ecs-graph` / `di-graph` skills
state; status maintenance, not discovery). Both graphs curate deterministically in their build
scripts (no LLM step), and each query auto-refreshes stale facts before printing, so a stale
graph self-heals to `curated: true` on the spot. From each report, note in one line per graph:
`curated`, stale files (code changed since the last build), and warnings count. If a graph reports
warnings or you suspect a rename left ghost nodes, offer the fix (`build_graph.py --force` /
`build_di_graph.py --force`) — do not run it unprompted.

### 1b. Doc-lint check

Run `python3 Tools/doc_lint.py --quiet` from the project root (1 cheap CLI call). It reports how many
ghost symbol claims the .md docs carry against the actual C# declarations. Note the one summary line
in the status report; if the count grew since the last session, say so. Do not fix ghosts unprompted —
the full list (`python3 Tools/doc_lint.py`) is on-demand ammunition for a doc-cleanup task, and a doc
that doc-lint flags is proof its claims must be re-verified against code before trusting them.

### 1c. Canon sync check

Run `python3 Tools/gen_agents.py --check` from the project root (1 cheap CLI call). It verifies that
every GENERATED zone in your autoloaded `AGENTS.md` is byte-identical to its SHARED canon block in
`CLAUDE.md`. Note the one summary line in the status report. On drift, offer the rebuild
(`python3 Tools/gen_agents.py`) — do not run it unprompted; canon edits themselves still land in
`CLAUDE.md` first.

### 2. Reconstruct the current state

From the `always` docs and any root status notes INDEX points to, extract only the facts that help
resume work:
- what baseline or milestone is described as complete
- what files or systems are called out as the current source of truth
- what the next roadmap item appears to be
- whether documents disagree about "what is next"

Treat active FLOWs as the authoritative resume artifacts:
- First read the Request's dated amendment log. Apply only entries marked confirmed to the current
  contract; surface unconfirmed amendments separately because they have no execution authority.
  If sources contradict one another, report the conflict instead of silently merging them.
- Prefer the Plan's explicit progress shape (`:status`, `:completed`, `:current`, `:remaining`,
  `:resume-context`, and conditional `:blocker`) when it exists. For an older FLOW without that
  shape, reconstruct the same facts from its existing Plan; do not rewrite the FLOW during startup.
- Exactly one active FLOW: report its current stage, confirmed amendments, unresolved decisions,
  disproven hypotheses (reread its **Disproven** section so the session never re-enters a dead end
  the FLOW already paid for — canon block `disproven` in `AGENTS.md`), completed/current/remaining
  work, and the smallest sufficient resume context. Ask whether to resume it or start a different task.
- A blocked FLOW remains active (`read: always`, `status: partial`), never complete. Report its
  blocker, the authority or external-state change needed, and its recorded next action.
- More than one active FLOW: report the conflict and list them; never merge their state or choose one.
- No active FLOW: use the normal roadmap/status reconstruction below.

When documents conflict, do not silently merge them — state the conflict explicitly with file names
and the differing claims. Treat process/safety rules in `AGENTS.md` as mandatory; treat dated status
notes as recency evidence; when two roadmap docs disagree, surface both and ask which is current.

### 3. Hand control back to the user

End the pass by asking the user what to do now — even if the next step seems obvious. The point is to
re-establish context first, then let the user choose the task.

## Output shape

Respond in the user's language. Keep it short and operational. Answers are prose — Clojure forms
only for artifacts, per the canon `agent-output` block in your autoloaded `AGENTS.md`:

1. `Read` — the docs you loaded (INDEX + the `always` set).
2. `Current state` — where work stopped and what is already done.
3. `Active FLOW` — its stage, confirmed amendments, progress, blocker if any, and next item; use
   `none` when absent and list all when several conflict.
4. `Possible next work` — the next roadmap item, or competing candidates if docs disagree.
5. `Question` — ask directly what to do next.

## Guardrails

- `INDEX.md` is the only entry point — do not wander the repo or read root `.md` files it does not
  send you to.
- Do not start coding / editing / running implementation during startup unless the user explicitly
  asks in the same request.
- Do not claim certainty about status when documents conflict.
- Do not expand into module-level docs unless INDEX / AGENTS.md require it for the present task.
