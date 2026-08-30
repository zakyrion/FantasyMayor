# FantasyMayor Session Start

## Overview

Orient via the generated doc map, reconstruct the current project state from it, then hand
control back to the user with a concise status summary and a direct question about the next task.
This is the **Research / orientation entry** of the Research → Plan → Execute lifecycle
(`.sdd-flow/FLOW_CONTRACT.md`, with this project's declarations in `.sdd-flow/project.md`) —
it does not plan or execute.

`INDEX.md` drives all navigation: do not preload anything it does not send you to, do not invent
a different onboarding flow, and do not start implementing before the user picks the next task —
unless they asked for implementation in the same request.

## Workflow

### 1. Load INDEX.md first — the doc map

Read `INDEX.md` before anything else (plain `Read`). It is the
single key to every doc + canvas: each one's read-priority (`always` / `trigger` / `reference`) plus
a one-line description.

Then follow INDEX's read-priority:
- Read every `read: always` doc next (currently `ARCHITECTURE.md`, `CLAUDE.md`).
  Execute the instructions inside `CLAUDE.md` — do not just summarize them.
  (`DOC_STANDARD.md` is `read: trigger`, not always — load it only when you author or review a doc
  yourself.)
- Open `trigger` docs only when their condition holds, and `reference` docs on demand —
  never preload them.

Respect the repository constraints you find (startup order, Unity build restrictions, module-folder
rules, read-on-demand references such as Addressables patterns).

### 1a. Graph health check

Run `python3 ~/.claude/skills/ecs-graph/scripts/ecsg.py stats` and
`python3 ~/.claude/skills/di-graph/scripts/dig.py stats` from the project root (2 cheap CLI calls —
status maintenance, not discovery; allowed for the main agent). Both graphs curate deterministically in
their build scripts (no LLM step), and each query auto-refreshes stale facts before printing, so a stale
graph self-heals to `curated: true` on the spot. From each report, note in one line per graph: `curated`,
stale files (code changed since the last build), and warnings count. If a graph reports warnings or you
suspect a rename left ghost nodes, offer the fix (`build_graph.py --force` / `build_di_graph.py --force`)
— do not run it unprompted.

### 1b. Doc-lint check

Run `python3 Tools/doc_lint.py --quiet` from the project root (1 cheap CLI call). It reports how many
ghost symbol claims the .md docs carry against the actual C# declarations. Note the one summary line
in the status report; if the count grew since the last session, say so. Do not fix ghosts unprompted —
the full list (`python3 Tools/doc_lint.py`) is on-demand ammunition for a doc-cleanup task, and a doc
that doc-lint flags is proof its claims must be re-verified against code before trusting them.

### 2. Reconstruct the current state

From the `always` docs and any root status notes INDEX points to, extract only the facts that help
resume work:
- what baseline or milestone is described as complete
- what files or systems are called out as the current source of truth
- what the next roadmap item appears to be
- whether documents disagree about "what is next"
- if a Category A FLOW with `status: partial` is active: its current stage, its **Findings** with
  their provenance (`:verified-by` + `:at` — a guess and a measurement must not read alike),
  unresolved decisions, its **Disproven** section (refuted hypotheses — reread them so the session
  never re-enters a dead end the FLOW already paid for), its **Attempted** section
  (tried-and-dropped approaches with the confidence they carried — an old approach is offered only
  as a named return), any linked `Flows/RESEARCH_*.md` document, and the next plan item. What each
  of those sections means is the canon: `.sdd-flow/FLOW_CONTRACT.md`

When documents conflict, do not silently merge them — state the conflict explicitly with file names
and the differing claims. Treat process/safety rules in `CLAUDE.md` as mandatory; treat dated status
notes as recency evidence; when two roadmap docs disagree, surface both and ask which is current.

### 3. Hand control back to the user

End the pass by asking the user what to do now — even if the next step seems obvious. The point is to
re-establish context first, then let the user choose the task.

## Output shape

Respond in the user's language. Keep it short and operational. Answers are prose — Clojure forms
only for artifacts, per the canon `agent-output` block in `.sdd-flow/FLOW_CONTRACT.md`:

1. `Read` — the docs you loaded (INDEX + the `always` set).
2. `Current state` — where work stopped and what is already done.
3. `Possible next work` — the next roadmap item, or competing candidates if docs disagree.
4. `Question` — ask directly what to do next.

## Guardrails

- `INDEX.md` is the only entry point — do not wander the vault or read root `.md` files it does not
  send you to.
- Do not start coding / editing / running implementation during startup unless the user explicitly
  asks in the same request.
- Do not claim certainty about status when documents conflict.
- Do not expand into module-level docs unless INDEX / CLAUDE require it for the present task.
