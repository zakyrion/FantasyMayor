---
category: C
read: always
tags: [contract, process, rules]
related:
  - "[INDEX](INDEX.md)"
  - "[ARCHITECTURE](ARCHITECTURE.md)"
  - "[DOC_STANDARD](DOC_STANDARD.md)"
---

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Working Contract: Research → Plan → Execute
Every engineering task runs in three phases. Each is already backed by an existing
gate — this names the discipline, it adds no new rule:
1. **Research** — gather facts, do not accumulate source. Discovery is delegated to
   the read-only scouts (see Discovery Scouts) or answered by the bounded
   `mcp__roslyn__*` tools and the ECS/DI graph CLIs (`ecsg.py` / `dig.py`); raw
   grep-family discovery by the main agent is a budgeted last resort (4 ops/task,
   `.claude/hooks/search-gate.py` counts them). In = distilled findings, not raw dumps.
2. **Plan** — restate the task via the Engineering Task Template, ask clarifying
   questions, and **wait for explicit confirmation** before any edit (the HARD GATE
   below). Immediately after confirmation run the new-task ritual:
   `python3 .claude/hooks/search-gate.py task <the .cs files named in «Працюй тільки в»>`
   — it re-arms the read+grep budgets for this task and grants the named files.
   Persist the plan as an on-disk artifact **only for multi-session programs**
   (a dedicated top-level plan doc); single-session tasks stay in plan-mode / the chat.
3. **Execute** — edit under the standing invariants (ECS writes via `Set()`,
   instance-by-default, zero-allocation systems). Reads-for-editing are budgeted by the
   same hook; on exhaustion, STOP and ask.

# FantasyMayor: Project Context & Architectural Decisions

## Start Working
- **Read `INDEX.md` first — and by default ONLY `INDEX.md`** (plain `Read`). It is the generated doc map and the single key to every doc and canvas: it carries each file's read-priority (`always` / `trigger` / `reference`) plus a one-line description. Let INDEX drive all navigation — do **not** preload anything it does not send you to.
- Follow INDEX's read-priority: read the docs it marks `read: always` next; open `trigger` docs only when their condition holds, and `reference` (per-module) docs on demand.
- `INDEX.md` is built in **2 passes**: (1) `python3 Tools/gen_index.py` rebuilds the structural skeleton between its `BEGIN/END GENERATED` markers from each doc's frontmatter + first line; (2) the agent curates descriptions / statuses / context. Re-run pass 1 after any frontmatter change; never edit between the markers, and keep the agent zone below the END marker short and informative.

## Documentation Access
- **This repo is an Obsidian vault, but plain file tools are the doc EDIT path.**
  Read/edit any `.md` via plain `Read` / `Edit` / `Write` — Obsidian sees on-disk
  changes. Do NOT edit via `vault_patch`/`vault_write` (heading-targeted patching
  against a moving doc was the project's top tool-error source). Move/delete via
  `vault_move` / `vault_delete` (they keep vault links intact) or flag the user.
- **Obsidian MCP is the doc SEARCH/NAVIGATION path:** `search_query` / `search_simple` /
  `vault_get_document_map`, plus a heading-scoped `vault_read` to pull ONE section of a
  large doc. If the server is down (Obsidian closed), `Grep` over `*.md` replaces search.
- **Code** files always use `Read` / `Edit` / `Write`.
- **Reading canvases:** by default read a `.canvas` via **`Tools/read_canvas.sh <file.canvas>`**
  (a `jq` projection — node `text`/`label` + edges, no positions); use full
  `vault_read` / `vault_write` only when you need to edit layout/positions.
- **`INDEX.md` is 2-pass, not free-form:** pass 1 — `gen_index.py` owns and rewrites
  the skeleton between the `BEGIN/END GENERATED` markers (never hand-edit there);
  pass 2 — the agent authors the zone below the END marker (preserved across runs).
  The Obsidian-write rule governs only that agent zone, not the generated skeleton.
- **Canvases** are JSONCanvas `.canvas` files catalogued automatically in INDEX's
  `Canvas map` (title = filename, desc = the canvas's group labels — add a group
  label to give a canvas a meaningful description). Convention: repo-root,
  `UPPER_SNAKE_CASE.canvas`.

## User Process Contract
- User-defined process and repository rules are mandatory and override agent-default workflows.
- Do not substitute personal heuristics, convenience patterns, or alternate execution flow when the user has already defined the process.
- Ignoring these instructions is considered a serious execution failure and must be treated as carrying complaint and legal escalation risk.

## Expected behavior
- You are my assistant. Your task is to implement my intent and propose your own options for solving problems.
- Our work is built on dialogue. I value discussing all important details in advance.
- Start work only when I explicitly instruct you to do so, or when you no longer have unresolved questions.
- Questions have higher priority than solving the task quickly.
- Use Context7 when I need library/API documentation or code generation, setup or configuration steps and you have some doubts about it.

## Discovery Scouts (the search front door)
- **The law is `.claude/SEARCH_POLICY.md`; the teeth are the hook `.claude/hooks/search-gate.py`.**
  The full decision table, budgets, and rationale live THERE — this section is only the behavioral
  rule; do not restate the details here.
- The main agent's raw source discovery over `Assets/**/*.cs` is BUDGETED, not free: 4 grep-family
  ops + 12 unique `.cs` reads per task (hook-counted; both re-armed by the `task` ritual). Grep is
  the last-resort fallback — the bounded tools and the scout come first. On exhaustion: reads →
  STOP and ask the user; greps → delegate to the scout. Subagents are exempt.
- **Exception:** the bounded `mcp__roslyn__*` tools return structured, not raw, output — the main
  agent MAY call them directly for surgical code-structure lookups.
- Delegate discovery to the read-only scouts in `.claude/agents/` (auto via their
  `description`, or explicitly `@agent-<name>`); they return distilled reports, never raw dumps:
  - **discovery-scout** (Sonnet) — the single front door: code structure (`roslyn-mcp`), ECS
    (`ecs-graph`), DI (`di-graph`), docs (Obsidian MCP).
  - **arch-scout** — `/arch-check` audit (stateful systems + collections bans); detector only.
  - **asset-scout** — `unity-asset-graph` queries (build contents, usage, dead assets, enum values).

## Doc Curation (the write path)
- The scouts above are read-only. Doc **authoring** has its own executor: **docs-curator** (Sonnet,
  `.claude/agents/docs-curator.md`) — the only write-capable agent. It owns `DOC_STANDARD.md`; the main
  agent does not read it to author docs.
- **Sync cadence is per-MILESTONE, not per-task.** The default is ONE batched curator run covering the
  whole branch delta, at a milestone / before merging the branch — not after every task. Mechanical
  facts self-heal in the interim (the graph CLIs auto-`--update`; code names are canonical), so a
  few days of semantic-doc lag is safe. Sync per-task only when the user explicitly asks.
- **The sync gate stands:** delegate AFTER code lands and the user approves the sync (finish code,
  STOP, ask, then delegate). Hand it a brief with four things: (1) what changed in code
  (symbols added/renamed/removed), (2) **why** — the intent/decisions only you hold, (3) which docs are
  likely affected, (4) what NOT to touch. For a milestone run the brief covers the branch delta —
  accumulate the per-task "why" bullets as you go so they are not lost. Invoke it scoped — an MD-sync
  run OR a graph STEP-2 run, not both in one call.
- It VALIDATES the brief against `DOC_STANDARD.md` rather than transcribing it — **drop freely** (strip
  tool-derivable facts), **verify freely** (check claims against the tools), **never invent** (a semantic
  gap the brief did not supply is flagged back, never reconstructed from source). So your brief must carry
  the "why"; if it omits it, the curator reports a gap instead of fabricating one.
- It authors Category A MDs + the INDEX pass-2 zone + graph STEP-2 curation only. Category C policy
  (`CLAUDE.md`, `ARCHITECTURE.md`, `ECS_CONVENTIONS.md`, `DOC_STANDARD.md`) and Category B patterns stay
  with the main agent / the user — the curator flags, never edits them. `ARCHITECTURE.md` is additionally
  **FROZEN** (`status: frozen`): NO agent edits it — the graph-gate hook turns an edit attempt into a
  user-approval ask; propose the change to the user instead.

## Engineering Task Template
- **HARD GATE — no actions before a confirmed task statement. For any engineering task you MUST first restate the task using the template below AND, if you have any doubt that you understood the task correctly, ask me your own clarifying questions in the same message. Then STOP and wait for my explicit confirmation. Only AFTER I confirm the statement may you create a plan or do any work. Forming a plan, entering plan mode, reading-for-implementation, or editing anything before that confirmation is a process violation. The duty to ask is yours: when in doubt, ask me — do not assume, and do not wait for me to question you. This overrides any default "just start planning" behavior.**
- Use the following template for engineering tasks by default. Engineering tasks include coding, architecture changes, refactors, module documentation, config-flow work, and other repository changes.
- Do not require this template for casual conversation or pure Q&A that does not ask for repository changes.
- Show this template to the user when they are defining an engineering task so they can see and reuse it.
- Expect engineering task requests to follow this format unless the user explicitly tells you to ignore it for the current request.
- If one or more blocks are missing in an engineering task request, do not silently invent them. Ask the user for each missing block separately and keep the discussion focused on filling those gaps.
- Blocks may be short, but every block should be present for engineering tasks unless the user explicitly opts out.

```text
Задача:
[що саме треба зробити]

Працюй тільки в:
- [файл/папка]
- [файл/папка]

Не дивись:
- [що поза scope]
- [що не треба аналізувати]

Роби за шаблоном:
- [Patterns/PATTERN_*.md — picker з описами: ARCHITECTURE.md → Pattern Recipes]
- [опціонально другий шаблон]

Архітектурні рішення:
- [що вже вирішено]
- [інваріанти, які не треба переосмислювати]

Не потрібно:
- [що не імплементувати / не перевіряти]

Результат:
- [який вихід очікується]
```

## Module MD Files
- Every module has an MD reference file in its root folder.
- **`DOC_STANDARD.md` (repo root) is the single source of truth for how every MD file is written.** It is
  `read: trigger` and owned by the **docs-curator** agent (see Doc Curation). The main agent reads it only
  when it authors/reviews a doc itself; the default path is to delegate doc authoring to the curator.
- **Before reading any source file in a module, read its MD file first.**
- Division of labor: **`roslyn-mcp`** (LSP) is the reference for code STRUCTURE — types, signatures, references, call/type hierarchy; **`ecs-graph`/`di-graph`** for ECS/DI relationships. Module MD files cover ONLY what those tools cannot extract: intent, non-obvious invariants, design decisions, how-to-use-correctly, and current state.
- Read source files only when both the MD and the tools (`roslyn-mcp` / `ecs-graph` / `di-graph`) lack the specific detail needed.
- If you change a module's invariants, public-usage rules, or current state, its MD file needs updating — do it via the **docs-curator** (brief it with the delta + the "why"); the curator applies it per `DOC_STANDARD.md`.
- If you add or change an ECS entity archetype, refresh the ecs-graph (`/ecs-graph`) — the sole archetype/event registry.
- Architecture policy and stack live in `ARCHITECTURE.md` (FROZEN — see Doc Curation); the living module/domain roster is `INDEX.md` → "Reference map"; point-of-code ECS/runtime conventions live in `ECS_CONVENTIONS.md`. Do not duplicate or override any of them in module MD files.

## Unity Build Policy
- This is a Unity project.
- Do not run Unity project builds from the agent side.
- Do not run `dotnet build`, `msbuild`, `xbuild`, or Unity CLI build commands for this repository.
- If compile validation is needed, request a Unity-side check from the user.
- Do not read Unity scene files such as `.unity` or other scene-serialized assets unless the user explicitly allows it in the current task.
- Never generate or hand-write Unity `.meta` files under any circumstances. If a `.meta` file is needed, stop and ask the user.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Prefer instance-based design; introduce `static` only when there is a clear architectural reason.

## Code Documentation Policy
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
