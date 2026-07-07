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
   below). Surface ALL open decisions in ONE consolidated pass and BATCH the questions —
   a stated effect whose value-source is not given (e.g. "add an id" with no id source) is an
   ask exactly like a missing field, never a licence to stub; do not drip questions across rounds.
   Immediately after confirmation run the new-task ritual:
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
  The decision table, budgets (12 `.cs` reads + 4 grep-family ops per task, re-armed by the `task`
  ritual), and escalation rules live THERE — do not restate them here.
- Discovery is delegated: **discovery-scout** (Sonnet) is the single front door; **arch-scout** and
  **asset-scout** cover arch-check and the asset graph (details in each `.claude/agents/*.md` —
  always set `subagent_type` explicitly). Every discovery-scout spawn uses the 4-field brief of
  SEARCH_POLICY §1b: question / anchors / shape / stop. Bounded `mcp__roslyn__*` calls are allowed directly; raw
  grep is a budgeted last resort. On exhaustion: reads → STOP and ask; greps → delegate to the
  scout. Subagents are exempt from the gates.

## Doc Curation (the write path)
- **docs-curator** (Sonnet, `.claude/agents/docs-curator.md`) is the ONLY write-capable agent and owns
  `DOC_STANDARD.md`. It VALIDATES briefs — drop tool-derivable, verify against tools, **never invent**
  — so every brief must carry the "why": (1) what changed, (2) intent/decisions only you hold,
  (3) affected docs, (4) what NOT to touch. A brief without the "why" yields a flagged gap, not a doc.
- **Cadence: ONE batched run per milestone / before merge, covering the branch delta** — never
  per-task unless the user asks (graphs auto-`--update` in the interim; days of semantic lag are safe).
  The gate stands: finish code, STOP, get the user's approval, then delegate — scoped (an MD-sync run
  OR a graph STEP-2 run, never both). Accumulate per-task "why" bullets for the milestone brief.
- It authors Category A MDs + the INDEX pass-2 zone + graph STEP-2 only. Category C policy and
  Category B patterns stay with the main agent / user — the curator flags, never edits them.
  `ARCHITECTURE.md` is additionally **FROZEN**: NO agent edits it — the graph-gate hook turns an
  attempt into a user-approval ask; propose the change to the user instead.

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

**EQUAL ALTERNATIVE — the LISP s-expr statement.** Same standing as the prose template;
the user picks either form per task. Expect and accept both:

```lisp
(task "title: A → B → C"                          ;; заголовок = ланцюжок потоку, не назва тікета
  (goal
    (mechanic-a → NEW thing, when/where it fires)
    (mechanic-b → NEW system; listens X → creates Y))
  ;; опційні поля — кожне присутнє знімає відповідне питання агента:
  (scope :only  → [файл/ папка/ …])               ;; = «Працюй тільки в»; вектор — список без ком
  (off-limits   → [що поза scope])                ;; = «Не дивись»
  (pattern      → Patterns/PATTERN_*.md)          ;; = «Роби за шаблоном»
  (decided      → що вже вирішено)                ;; = «Архітектурні рішення»
  {:skip що-не-робити  :result очікуваний-вихід}) ;; мапа дрібних полів = «Не потрібно» + «Результат»
```

- The HARD GATE and the missing-block rules apply UNCHANGED: an absent field means "ask about
  that block, aiming the question at the specific `goal` bracket" — never "no constraints".
- The notation is defined ONCE — do not re-explain it here or anywhere else. The canonical
  glossary (all operators/literals, with examples): `~/.claude/CLAUDE.md` → "LISP/Clojure-like
  task notation", already in every agent's context; authoring spec for s-expr rules inside
  docs: `DOC_STANDARD.md` → Rule Style.

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
- **Pre-check BEFORE asking:** first run `mcp__roslyn__get_diagnostics` (solutionPath:
  `FantasyMayor.sln`, scoped to the edited project/files) and fix what it reports. Unity stays the
  authority — roslyn's workspace misses codegen and types added/renamed since the last Unity regen —
  so the pre-check filters plain C# errors out of the round-trip; it never replaces the user's check.
- Do not read Unity scene files such as `.unity` or other scene-serialized assets unless the user explicitly allows it in the current task.
- Never generate or hand-write Unity `.meta` files under any circumstances. If a `.meta` file is needed, stop and ask the user.

## Code Quality And Review Bar
- Write code that is ready to pass strict review.
- Review incoming code as if done by a senior engineer with 20 years of experience in a very critical mood.
- Prefer instance-based design; introduce `static` only when there is a clear architectural reason.
- Default to the SIMPLEST structure that solves the task. Patterns (orchestrator/subsystem fan-out, snapshot-before-iterate, …) serve the problem — reach for one ONLY when cardinality or real complexity demands it, never because a doc or code comment mentions it. A one-element set needs no snapshot; a one-line tag swap needs no subsystem family.
- Never ship a knowingly-wrong placeholder (e.g. a hardcoded id) behind "out of scope". When a value needs a real source you do not yet have, surface it as a decision during Plan — do not implement a stub and present it as done.

## Code Documentation Policy
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
