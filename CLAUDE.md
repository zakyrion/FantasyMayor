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
   `mcp__roslyn__*` / `mcp__ecs-graph__*` / `mcp__di-graph__*` tools; the main agent
   does **not** do raw source discovery (`.claude/hooks/search-gate.py` enforces it).
   In = distilled findings, not raw dumps.
2. **Plan** — restate the task via the Engineering Task Template, ask clarifying
   questions, and **wait for explicit confirmation** before any edit (the HARD GATE
   below). Persist the plan as an on-disk artifact **only for multi-session programs**
   (a dedicated top-level plan doc); single-session tasks stay in plan-mode / the chat.
3. **Execute** — edit under the standing invariants (ECS writes via `Set()`,
   instance-by-default, zero-allocation systems). Reads-for-editing are budgeted by the
   same hook; on exhaustion, STOP and ask.

# FantasyMayor: Project Context & Architectural Decisions

## Start Working
- **Read `INDEX.md` first — and by default ONLY `INDEX.md`** (via Obsidian MCP `vault_read`; fallback plain `Read`). It is the generated doc map and the single key to every doc and canvas: it carries each file's read-priority (`always` / `trigger` / `reference`) plus a one-line description. Let INDEX drive all navigation — do **not** preload anything it does not send you to.
- Follow INDEX's read-priority: read the docs it marks `read: always` next; open `trigger` docs only when their condition holds, and `reference` (per-module) docs on demand.
- `INDEX.md` is built in **2 passes**: (1) `python3 Tools/gen_index.py` rebuilds the structural skeleton between its `BEGIN/END GENERATED` markers from each doc's frontmatter + first line; (2) the agent curates descriptions / statuses / context. Re-run pass 1 after any frontmatter change; never edit between the markers, and keep the agent zone below the END marker short and informative.

## Documentation Access (Obsidian-first)
- **This repo is an Obsidian vault.** The main agent reads/edits docs (`.md`) and
  canvases (`.canvas`) through the **Obsidian MCP** (`mcp__obsidian__*`): read
  `vault_read` (heading/block/frontmatter targeting) · write/edit `vault_write` /
  `vault_patch` / `vault_append` · move/delete `vault_move` / `vault_delete`.
  (Doc/code *discovery & search* is delegated to the scout — see Discovery Scouts.)
- **Fallback:** if the `obsidian` server is not connected (Obsidian closed / HTTP
  server off), use plain `Read` / `Write` / `Edit`. The MCP path needs Obsidian running.
- **Scope:** Obsidian-first applies to docs (`.md`) and canvases (`.canvas`) only.
  **Code** files always use `Read` / `Edit` / `Write`.
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
**Discovery is delegated to dedicated read-only Haiku subagents in `.claude/agents/`** —
the main agent does NOT do raw source discovery; it spends its budget on reasoning,
decisions, and edits. **Exception:** the bounded `mcp__roslyn__*` tools (definition,
references, symbols, outline, call/type hierarchy) return structured, not raw, output —
the main agent MAY call them directly for surgical code-structure lookups. Delegate
heavier traces (auto via their `description`, or explicitly `@agent-<name>`):
- **discovery-scout** — the **single front door** for ECS/DI/orchestration/docs discovery
  and multi-step traces: general code structure (definition, refs, symbols, call/type
  hierarchy — **`roslyn-mcp`**, `mcp__roslyn__*`), DoD/ECS (archetypes, component
  writers/readers, reactive consumers, producer→consumer, Table-Rule PK/FK, system roles
  — `ecs-graph`), DI/VContainer (registered-as + Lifetime + installer, injectors,
  collection resolution, system→`GameMode` — `di-graph`), AND **docs** (Obsidian MCP
  search, clean discovery that avoids `Library/`/`Packages/`/plugins). Returns distilled
  findings, not raw dumps.
- **arch-scout** — `arch-check` audit (stateful systems + System.Collections.Generic
  bans); detector only.
- **asset-scout** — `unity-asset-graph` queries (build contents, asset usage,
  dead/unused, serialized enum values).

All three are read-only (no Edit/Write) and return distilled reports; the main agent
keeps the reasoning, decisions, and edits. **The law is `.claude/SEARCH_POLICY.md`,
hook-enforced** (`.claude/hooks/search-gate.py`): in the main session, raw source
discovery over `Assets/**/*.cs` + direct graph CLIs (`ecsg`/`dig`) are denied and routed
to the bounded `mcp__roslyn__*` tools / the scout, and per-session `.cs` reads are
budgeted (on exhaustion, STOP and ask the user). Subagents are exempt.

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

Роби за аналогією з:
- [еталонний файл/клас/модуль]
- [опціонально другий еталон]

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
- **`DOC_STANDARD.md` (repo root) is the single source of truth for how every MD file is written.** Read it before creating or editing any `.md`.
- **Before reading any source file in a module, read its MD file first.**
- Division of labor: **`roslyn-mcp`** (LSP) is the reference for code STRUCTURE — types, signatures, references, call/type hierarchy; **`ecs-graph`/`di-graph`** for ECS/DI relationships. Module MD files cover ONLY what those tools cannot extract: intent, non-obvious invariants, design decisions, how-to-use-correctly, and current state.
- Read source files only when both the MD and the tools (`roslyn-mcp` / `ecs-graph` / `di-graph`) lack the specific detail needed.
- If you change a module's invariants, public-usage rules, or current state, update its MD file per `DOC_STANDARD.md`.
- If you add or change an ECS entity archetype, refresh the ecs-graph (`/ecs-graph`) — the sole archetype/event registry.
- Architecture, stack, module layout, and ECS conventions are described in `ARCHITECTURE.md`. Do not duplicate or override architecture rules in module MD files.

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
