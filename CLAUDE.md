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

# FantasyMayor: Project Context & Architectural Decisions

## Graphify Scope

**Full rebuild** (done once): scan `Assets/` + root MD files.
```
graphify Assets/  (+ ARCHITECTURE.md, GAMEPLAY_FOUNDATION.md, SYSTEMTEMPLATE.md, CONFIGTEMPLATE.md, ECS_REFERENCE.md)
```

**Incremental `--update`**: limit to game code only — `Assets/Modules`, `Assets/Scripts`, and root MD files.
Third-party folders (Plugins, Packages, TextMesh Pro, Easy Save 3, Dreamteck, Sirenix, etc.) are immutable — never re-scan them on updates.

For `--update`, use **Gemini** as primary. Fallbacks if Gemini quota is exhausted:
```bash
# Primary — Gemini (free tier, 20 req/day)
/graphify Assets/Modules --update --backend gemini

# Fallback 1 — Codex via OpenRouter (free tier, OpenAI-compatible)
export OLLAMA_BASE_URL=https://openrouter.ai/api/v1
export OLLAMA_API_KEY=<openrouter_key>
export GRAPHIFY_OLLAMA_MODEL=openai/codex-mini:free
/graphify Assets/Modules --update --backend ollama

# Fallback 2 — claude-cli (uses this Claude Code session, no extra cost)
/graphify Assets/Modules --update --backend claude-cli
```

When Gemini fails (503, quota, or any error) and a subagent fallback is needed, always use `model: "haiku"` — it is the cheapest available Claude model. Never spawn a fallback subagent without explicitly setting the model to haiku. Same rule applies when using OpenAI-compatible backends (OpenRouter, Ollama): always pick the cheapest/free model tier available.

## Start Working
- **Read `INDEX.md` first.** It is the generated doc map and the single source of the start-reading list.
- Read every doc it marks `read: always` (currently `ARCHITECTURE.md`, `DOC_STANDARD.md`, `GAMEPLAY_FOUNDATION.md`).
- Do **not** preload anything else. `INDEX.md` lists `trigger` docs (read only when their condition holds — e.g. `SYSTEMTEMPLATE.md`, `CONFIGTEMPLATE.md`, `ECS_REFERENCE.md`, `GENERAL_UI_STYLE.md`, `ADDRESSABLE_PATTERNS.md`) and `reference` docs (per-module, on demand). Glance at the index, then decide.
- `INDEX.md` is generated — never hand-edit it. After changing any doc's frontmatter, regenerate: `python3 Tools/gen_index.py`.

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

### Codebase Search Budget
## Graphify Search Policy

Goal:
- minimize direct source-code reading
- minimize token usage

Execution formula:
- unknown name -> `rg` -> `graphify`
- known name -> `graphify`
- source read -> verification only

Rules:
1. Do not read `GRAPH_REPORT.md` for point lookups.
   Use it only for broad architecture orientation or graph-quality review.
2. If the module is already known from the task scope, read its module MD first.
   Symbols found there count as free discovery and do not require `rg`.
3. If the exact symbol name is already known, start with `graphify`.
   Do not run `rg` first.
4. If the exact symbol name is unknown, run exactly one narrow `rg` to discover the canonical symbol name.
   After that, switch to `graphify`.
5. Use Graphify commands by role:
   - `graphify explain "X"` -> symbol lookup and immediate connections
   - `graphify path "A" "B"` -> flow / dependency / orchestration chain
   - `graphify affected "X"` -> impact analysis / blast radius
   - `graphify query "..."` -> relation-shaped questions only
6. In `graphify query`, use exact labels, not broad natural language.
   Preferred forms:
   - `what calls X`
   - `what imports X`
   - `what references X`
   - `what returns X`
7. Prefer `graphify path "A" "B"` over `graphify query --dfs` for:
   "How is A connected to B?"
8. Read source code only after `graphify` has narrowed the target to a specific source file.
   Read only the minimum necessary file and fragment.
   Use `source_location` when available.
   If `graphify` resolves only to docs or concept nodes, source reads are still allowed only when the graph evidence clearly identifies the target source file.
9. If `graphify` returns an ambiguous or empty result, narrow the symbol name and retry.
   Do not silently fall back to broad source-code reading.
   If the graph does not contain the answer, say so explicitly.

Task budget:
- `rg`: max 1
- `graphify`: unlimited
- module MD files: unlimited
- source file reads: max 1, only after graph narrowing

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
- Division of labor: `graphify` is the reference for code STRUCTURE — types, signatures, dependencies, inheritance, priorities. Module MD files cover ONLY what `graphify` cannot extract: intent, non-obvious invariants, design decisions, how-to-use-correctly, and current state.
- Read source files only when both the MD and `graphify` lack the specific detail needed.
- If you change a module's invariants, public-usage rules, or current state, update its MD file per `DOC_STANDARD.md`.
- If you add or change an ECS entity archetype, update the central registry `ECS_REFERENCE.md`.
- Architecture, stack, module layout, and ECS conventions are described in `ARCHITECTURE.md`. Do not duplicate or override architecture rules in module MD files.

## Terrain / Isoline Pre-read
Before modifying terrain transitions, read these files first:
- `Assets/Presentation/Terrain/Isolines/FieldBasedIsolineBuilder.cs`
- `Assets/Presentation/Terrain/Isolines/IsolineSlopeTransition.cs`
- `Assets/Presentation/Terrain/Smooth/HeightSmoothing.cs`

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

## Patterns Reference
- **Addressables / `IAddressable` / `Box<T>` / `Result<T>` / addressable asset loading:** before writing, editing, or reviewing any such code, read `Assets/Modules/Addressable/ADDRESSABLE_PATTERNS.md` first. It is the single source of truth — do not re-derive patterns from source, do not deviate without user approval. The file is intentionally not loaded into context by default; load it on demand when the trigger applies.
- **ECS queries / entity tables / joins / `EntityMap` / `EntityMultiMap`:** before writing or reviewing any ECS query, follow the Table Rule in `ARCHITECTURE.md` ("Relational Modeling — Table Rule"). Core rule: a query names a table = key component + discriminator; a bare `With<KeyComponent>` query is forbidden. PK table → `EntityMap`, FK 1:N → `EntityMultiMap`, sweep → `EntitySet`. Full rules live in `ARCHITECTURE.md` only — do not duplicate them elsewhere.

## Code Documentation Policy
- Add comments only where the logic stops being simple and unambiguous.
- Prefer short targeted comments for non-obvious algorithmic constraints, decisions, and invariants.
- Do not add boilerplate XML documentation by default.
